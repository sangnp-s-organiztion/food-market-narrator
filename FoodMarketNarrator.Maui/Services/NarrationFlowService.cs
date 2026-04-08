using Microsoft.Maui.Devices.Sensors;
using food_market_narrator.Models;
using food_market_narrator.Settings;

namespace food_market_narrator.Services;

public class NarrationFlowService : INarrationFlowService
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly IAudioService _audioService;
    private readonly IAudioLogSyncService _audioLogSyncService;
    private readonly ILanguageService _languageService;
    private readonly IHistoryService _historyService;
    private readonly ILocationLogSyncService _locationLogSyncService;
    private readonly IQrAccessService _qrAccessService;

    // Track POI đã phát audio trong phiên
    private readonly HashSet<string> _playedPOIs = new();

    // Cooldown: thời gian tối thiểu giữa các lần phát cho cùng POI (60 giây)
    private readonly Dictionary<string, DateTime> _poiLastPlayedTime = new();

    // Debounce: vị trí cuối cùng đã xử lý
    private Location? _lastProcessedLocation = null;
    private const double MinDistanceToProcess = 5.0; // mét

    private bool _isNarrationEnabled = false;

    private readonly Queue<NarrationQueueItem> _playQueue = new();
    private bool _isProcessingQueue = false;
    private string? _currentPlayingPoiId;
    private readonly object _autoNarrationScopeLock = new();
    private HashSet<string>? _autoNarrationScopedPoiIds;
    private CancellationTokenSource? _switchCutoffCts;
    private static readonly TimeSpan PoiSwitchCutoffDelay = TimeSpan.FromSeconds(3);
    private CancellationTokenSource? _qrGuardCts;
    public bool IsNarrating => _isNarrationEnabled;

    public NarrationFlowService(
        IPOIService poiService,
        ILocationService locationService,
        IAudioService audioService,
        IAudioLogSyncService audioLogSyncService,
        ILanguageService languageService,
        IHistoryService historyService,
        ILocationLogSyncService locationLogSyncService,
        IQrAccessService qrAccessService)
    {
        _poiService = poiService;
        _locationService = locationService;
        _audioService = audioService;
        _audioLogSyncService = audioLogSyncService;
        _languageService = languageService;
        _historyService = historyService;
        _locationLogSyncService = locationLogSyncService;
        _qrAccessService = qrAccessService;
    }

    public void StartNarration()
    {
        if (_isNarrationEnabled) return;

        _isNarrationEnabled = true;

        // Reset trạng thái khi bắt đầu phiên mới
        _playedPOIs.Clear();
        _poiLastPlayedTime.Clear();
        _lastProcessedLocation = null;
        _poiService.ResetGeofenceState();

        _locationService.LocationChanged += OnLocationChanged;
        _ = _locationService.StartTrackingAsync();
        StartQrGuardLoopIfNeeded();

        var cachedLocation = _locationService.LastKnownLocation;
        if (cachedLocation != null)
        {
            _lastProcessedLocation = cachedLocation;
            _ = CheckAndNarrateAsync(cachedLocation);
            return;
        }

        // Fallback khi chưa có vị trí cache: lấy vị trí một lần ở nền
        _ = Task.Run(async () =>
        {
            var currentLocation = await _locationService.GetCurrentLocationAsync();
            if (currentLocation != null && _isNarrationEnabled)
            {
                _lastProcessedLocation = currentLocation;
                await CheckAndNarrateAsync(currentLocation);
            }
        });
    }

    public void SetAutoNarrationPoiScope(IEnumerable<string>? poiIds)
    {
        HashSet<string>? normalizedScope = null;
        if (poiIds != null)
        {
            var normalized = poiIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);

            if (normalized.Count > 0)
            {
                normalizedScope = normalized;
            }
        }

        lock (_autoNarrationScopeLock)
        {
            _autoNarrationScopedPoiIds = normalizedScope;
        }

        // Scope narration cần reset geofence để tránh state POI trước đó làm lệch trigger.
        _poiService.ResetGeofenceState();
    }

    public void ClearAutoNarrationPoiScope()
    {
        lock (_autoNarrationScopeLock)
        {
            _autoNarrationScopedPoiIds = null;
        }

        _poiService.ResetGeofenceState();
    }

    public void StopNarration()
    {
        if (!_isNarrationEnabled) return;

        _isNarrationEnabled = false;

        // stop tracking
        _locationService.LocationChanged -= OnLocationChanged;

        // stop audio
        _audioService.StopSound();
        _qrGuardCts?.Cancel();
        _qrGuardCts = null;
        _switchCutoffCts?.Cancel();
        _switchCutoffCts = null;
        _currentPlayingPoiId = null;

        // clear queue
        _playQueue.Clear();
        _isProcessingQueue = false;

        // reset POI đã phát để lần Start tiếp theo có thể phát lại ở cùng vị trí
        _playedPOIs.Clear();
        _poiLastPlayedTime.Clear();
        _lastProcessedLocation = null;
        _poiService.ResetGeofenceState();
    }

    // Khi thay đổi vị trí thì làm gì đó
    private async void OnLocationChanged(object? sender, Location location)
    {
        // Debounce: bỏ qua nếu di chuyển quá ngắn
        if (_lastProcessedLocation != null)
        {
            var distance = Location.CalculateDistance(
                _lastProcessedLocation,
                location,
                DistanceUnits.Kilometers) * 1000;

            if (distance < MinDistanceToProcess)
            {
                return;
            }
        }

        _lastProcessedLocation = location;
        await CheckAndNarrateAsync(location);
    }

    public async Task CheckAndNarrateAsync(Location? currentLocation = null, bool force = false)
    {
        if (_isNarrationEnabled && !await EnsureQrAccessAsync())
        {
            StopNarration();
            return;
        }

        if (currentLocation == null)
            currentLocation = await _locationService.GetCurrentLocationAsync();

        if (currentLocation == null)
        {
            return;
        }

        var pois = await _poiService.GetAllPOIsAsync();
        if (pois == null || !pois.Any())
        {
            return;
        }

        var scopedPois = ApplyAutoNarrationScope(pois, force);
        if (scopedPois.Count == 0)
        {
            return;
        }

        // SỬ DỤNG GEOFENCE TRANSITION từ POIService
        // UpdateNearestPOI trả về POI mới khi:
        // - Enter vào POI (lần đầu vào radius 30m)
        // - Chuyển từ POI này sang POI khác (cả hai trong radius 30m)
        var newPoi = _poiService.UpdateNearestPOI(
            currentLocation.Latitude,
            currentLocation.Longitude,
            scopedPois);

        // Nếu có POI mới từ geofence transition HOẶC force trigger
        if (newPoi != null || force)
        {
            // Nếu force, dùng nearest POI
            var targetPoi = force
                ? _poiService.GetNearestPOI(currentLocation, scopedPois)
                : newPoi;

            if (targetPoi == null)
            {
                return;
            }

            await TryPlayAudioAsync(targetPoi, currentLocation, force);
        }
    }

    private List<POI> ApplyAutoNarrationScope(IEnumerable<POI> pois, bool force)
    {
        if (force)
        {
            return pois.ToList();
        }

        if (!IsMapPageRouteActive())
        {
            // Chỉ áp dụng scope trong lúc người dùng thực sự đang ở MapPage.
            return pois.ToList();
        }

        HashSet<string>? activeScope;
        lock (_autoNarrationScopeLock)
        {
            activeScope = _autoNarrationScopedPoiIds;
        }

        if (activeScope == null || activeScope.Count == 0)
        {
            return pois.ToList();
        }

        return pois
            .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId) && activeScope.Contains(p.restaurantId))
            .ToList();
    }

    private static bool IsMapPageRouteActive()
    {
        var route = Shell.Current?.CurrentState?.Location?.ToString();
        return !string.IsNullOrWhiteSpace(route)
            && route.Contains("MapPage", StringComparison.OrdinalIgnoreCase);
    }

    private async Task TryPlayAudioAsync(POI poi, Location currentLocation, bool force = false)
    {
        var selectedAudio = ResolveSelectedAudio(poi, _languageService.CurrentLanguage);
        if (selectedAudio == null || string.IsNullOrWhiteSpace(selectedAudio.AudioUrl))
        {
            return;
        }

        var poiId = poi.restaurantId;
        var distanceMeters = _poiService.GetDistanceMeters(currentLocation, poi);

        // Kiểm tra khoảng cách (chỉ cho auto trigger)
        if (!force && distanceMeters > AppSettings.TriggerDistanceMeters)
        {
            return;
        }

        // Kiểm tra cooldown: đã phát gần đây chưa?
        if (_poiLastPlayedTime.TryGetValue(poiId, out var lastPlayedTime))
        {
            var cooldownSeconds = (DateTime.Now - lastPlayedTime).TotalSeconds;
            if (!force && cooldownSeconds < 60) // 60 giây cooldown
            {
                return;
            }
        }

        // Kiểm tra đã phát trong phiên chưa (chỉ cho auto)
        var alreadyPlayed = _playedPOIs.Contains(poiId);

        // Force luôn cho phép phát lại POI hiện tại
        if (force || !alreadyPlayed)
        {
            var shouldInterruptForPoiSwitch = !force
                && _audioService.IsPlaying
                && _isProcessingQueue
                && !string.IsNullOrWhiteSpace(_currentPlayingPoiId)
                && !string.Equals(_currentPlayingPoiId, poiId, StringComparison.OrdinalIgnoreCase);

            if (shouldInterruptForPoiSwitch)
            {
                // Khi chuyển sang POI mới trong lúc audio cũ đang phát,
                // chỉ giữ lại POI mới trong queue để phát ngay sau khi cắt audio cũ.
                _playQueue.Clear();
            }

            // Thêm vào queue để phát
            _playQueue.Enqueue(new NarrationQueueItem
            {
                Poi = poi,
                AudioId = selectedAudio.AudioId,
                AudioUrl = selectedAudio.AudioUrl
            });

            if (!alreadyPlayed)
            {
                _playedPOIs.Add(poiId);
            }

            // Cập nhật thời gian phát gần nhất
            _poiLastPlayedTime[poiId] = DateTime.Now;

            if (shouldInterruptForPoiSwitch)
            {
                ScheduleCutoffForPoiSwitch(_currentPlayingPoiId!, poiId);
            }

            await ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessingQueue)
            return;

        _isProcessingQueue = true;

        while (_playQueue.Count > 0)
        {
            // Nếu đang có track phát, chờ phát xong rồi mới lấy item kế tiếp trong queue.
            while (_audioService.IsPlaying)
            {
                await Task.Delay(300);
            }

            var queueItem = _playQueue.Dequeue();
            var poi = queueItem.Poi;
            _currentPlayingPoiId = poi.restaurantId;
            DateTime? startedAtUtc = null;
            await _audioService.PlaySound(queueItem.AudioId);

            if (await WaitForPlaybackStartAsync())
            {
                startedAtUtc = DateTime.UtcNow;
            }

            // Khi audio auto narration đã bắt đầu phát thành công, lưu POI vào lịch sử.
            if (startedAtUtc.HasValue && !string.IsNullOrWhiteSpace(poi.restaurantId))
            {
                _historyService.AddToHistory(poi.restaurantId);
            }

            // Chờ audio phát xong
            while (_audioService.IsPlaying)
            {
                await Task.Delay(300);
            }

            if (startedAtUtc.HasValue && queueItem.AudioId > 0 && !string.IsNullOrWhiteSpace(poi.restaurantId))
            {
                var endedAtUtc = DateTime.UtcNow;
                await _audioLogSyncService.LogPlaybackAsync(
                    poi.restaurantId,
                    queueItem.AudioId,
                    startedAtUtc.Value,
                    endedAtUtc);
            }

            _currentPlayingPoiId = null;
        }

        _isProcessingQueue = false;
    }

    private void ScheduleCutoffForPoiSwitch(string fromPoiId, string toPoiId)
    {
        _switchCutoffCts?.Cancel();
        var cts = new CancellationTokenSource();
        _switchCutoffCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(PoiSwitchCutoffDelay, cts.Token);

                if (cts.IsCancellationRequested || !_isNarrationEnabled)
                {
                    return;
                }

                if (_audioService.IsPlaying
                    && string.Equals(_currentPlayingPoiId, fromPoiId, StringComparison.OrdinalIgnoreCase))
                {
                    _audioService.StopSound();
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            finally
            {
                if (ReferenceEquals(_switchCutoffCts, cts))
                {
                    _switchCutoffCts = null;
                }

                cts.Dispose();
            }
        });
    }

    private async Task<bool> WaitForPlaybackStartAsync(int timeoutMs = 2000)
    {
        const int pollDelayMs = 100;
        var waitedMs = 0;

        while (waitedMs < timeoutMs)
        {
            if (_audioService.IsPlaying)
            {
                return true;
            }

            await Task.Delay(pollDelayMs);
            waitedMs += pollDelayMs;
        }

        return _audioService.IsPlaying;
    }

    public void ResetPlayedPOIs()
    {
        _playedPOIs.Clear();
        _poiLastPlayedTime.Clear();
    }

    private void StartQrGuardLoopIfNeeded()
    {
        if (!_qrAccessService.IsQrTimeRestricted)
        {
            return;
        }

        _qrGuardCts?.Cancel();
        _qrGuardCts = new CancellationTokenSource();
        var token = _qrGuardCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && _isNarrationEnabled)
            {
                if (!await EnsureQrAccessAsync())
                {
                    StopNarration();
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private async Task<bool> EnsureQrAccessAsync()
    {
        if (!_qrAccessService.IsQrTimeRestricted)
        {
            return true;
        }

        var sessionId = _locationLogSyncService.CurrentSessionId;
        return await _qrAccessService.CanContinueNarrationAsync(sessionId);
    }

    private static AudioModel? ResolveSelectedAudio(POI poi, string languageCode)
    {
        var activeAudios = poi.Audios
            .Where(a => a.IsActive)
            .ToList();

        var byLanguage = activeAudios
            .Where(a => string.Equals(a.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Version)
            .ThenByDescending(a => a.DateGeneration)
            .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.AudioUrl));

        if (byLanguage != null)
        {
            return byLanguage;
        }

        return activeAudios
            .OrderByDescending(a => a.Version)
            .ThenByDescending(a => a.DateGeneration)
            .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.AudioUrl));
    }
}

internal class NarrationQueueItem
{
    public POI Poi { get; set; } = new();
    public int AudioId { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
}

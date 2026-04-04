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
    public bool IsNarrating => _isNarrationEnabled;

    public NarrationFlowService(
        IPOIService poiService,
        ILocationService locationService,
        IAudioService audioService,
        IAudioLogSyncService audioLogSyncService,
        ILanguageService languageService,
        IHistoryService historyService)
    {
        _poiService = poiService;
        _locationService = locationService;
        _audioService = audioService;
        _audioLogSyncService = audioLogSyncService;
        _languageService = languageService;
        _historyService = historyService;
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

    public void StopNarration()
    {
        if (!_isNarrationEnabled) return;

        _isNarrationEnabled = false;

        // stop tracking
        _locationService.LocationChanged -= OnLocationChanged;

        // stop audio
        _audioService.StopSound();

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
        // Nếu đang phát audio, bỏ qua (trừ force)
        if (!force && _audioService.IsPlaying)
        {
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

        // SỬ DỤNG GEOFENCE TRANSITION từ POIService
        // UpdateNearestPOI trả về POI mới khi:
        // - Enter vào POI (lần đầu vào radius 30m)
        // - Chuyển từ POI này sang POI khác (cả hai trong radius 30m)
        var newPoi = _poiService.UpdateNearestPOI(currentLocation.Latitude, currentLocation.Longitude);

        // Nếu có POI mới từ geofence transition HOẶC force trigger
        if (newPoi != null || force)
        {
            // Nếu force, dùng nearest POI
            var targetPoi = force
                ? _poiService.GetNearestPOI(currentLocation, pois)
                : newPoi;

            if (targetPoi == null)
            {
                return;
            }

            await TryPlayAudioAsync(targetPoi, currentLocation, force);
        }
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
            var queueItem = _playQueue.Dequeue();
            var poi = queueItem.Poi;
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
        }

        _isProcessingQueue = false;
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

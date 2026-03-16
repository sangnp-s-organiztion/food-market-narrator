using Microsoft.Maui.Devices.Sensors;
using food_market_narrator.Models;
using food_market_narrator.Settings;

namespace food_market_narrator.Services;

public class NarrationFlowService : INarrationFlowService
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly IAudioService _audioService;
    private readonly ILanguageService _languageService;

    // Track POI đã phát audio trong phiên
    private readonly HashSet<string> _playedPOIs = new();

    // Cooldown: thời gian tối thiểu giữa các lần phát cho cùng POI (60 giây)
    private readonly Dictionary<string, DateTime> _poiLastPlayedTime = new();

    // Debounce: vị trí cuối cùng đã xử lý
    private Location? _lastProcessedLocation = null;
    private const double MinDistanceToProcess = 5.0; // mét

    private bool _isNarrationEnabled = false;

    private readonly Queue<POI> _playQueue = new();
    private bool _isProcessingQueue = false;
    public bool IsNarrating => _isNarrationEnabled;

    public NarrationFlowService(
        IPOIService poiService,
        ILocationService locationService,
        IAudioService audioService,
        ILanguageService languageService)
    {
        _poiService = poiService;
        _locationService = locationService;
        _audioService = audioService;
        _languageService = languageService;
    }

    public async void StartNarration()
    {
        if (_isNarrationEnabled) return;

        _isNarrationEnabled = true;

        // Reset trạng thái khi bắt đầu phiên mới
        _playedPOIs.Clear();
        _poiLastPlayedTime.Clear();
        _lastProcessedLocation = null;

        _locationService.LocationChanged += OnLocationChanged;
        await _locationService.StartTrackingAsync();

        // Kiểm tra ngay lần đầu
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation != null)
        {
            _lastProcessedLocation = currentLocation;
            await CheckAndNarrateAsync(currentLocation);
        }
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
        var selectedAudio = poi.GetAudioUrl(_languageService.CurrentLanguage);

        if (string.IsNullOrWhiteSpace(selectedAudio))
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
            _playQueue.Enqueue(poi);

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
            var poi = _playQueue.Dequeue();

            var selectedAudio = poi.GetAudioUrl(_languageService.CurrentLanguage);
            if (string.IsNullOrWhiteSpace(selectedAudio))
            {
                continue;
            }

            await _audioService.PlaySound(
                _languageService.CurrentLanguage,
                selectedAudio
            );

            // Chờ audio phát xong
            while (_audioService.IsPlaying)
            {
                await Task.Delay(300);
            }
        }

        _isProcessingQueue = false;
    }

    public void ResetPlayedPOIs()
    {
        _playedPOIs.Clear();
        _poiLastPlayedTime.Clear();
    }
}

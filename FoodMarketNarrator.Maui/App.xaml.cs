using food_market_narrator.Services;
using food_market_narrator.Settings;
using System.Diagnostics;

namespace food_market_narrator;

public partial class App : Application
{
    private readonly ILocationService _locationService;
    private readonly ILocationLogSyncService _locationLogSyncService;
    private readonly IPOIService _poiService;
    private readonly ITourService _tourService;
    private readonly ILanguageService _languageService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IQrAccessService _qrAccessService;
    private readonly NarrationFlowService _narrationFlowService;
    private bool _warmupStarted;
    private CancellationTokenSource? _qrAccessGuardCts;

    public App(
        ILocationService locationService,
        ILocationLogSyncService locationLogSyncService,
        IPOIService poiService,
        ITourService tourService,
        ILanguageService languageService,
        IAudioLibraryService audioLibraryService,
        IQrAccessService qrAccessService,
        NarrationFlowService narrationFlowService)
	{
		InitializeComponent();
        _locationService = locationService;
		_locationLogSyncService = locationLogSyncService;
		_poiService = poiService;
        _tourService = tourService;
		_languageService = languageService;
        _audioLibraryService = audioLibraryService;
        _qrAccessService = qrAccessService;
        _narrationFlowService = narrationFlowService;

        AppLinkDispatcher.DeepLinkReceived += OnDeepLinkReceived;

        // Xử lý deep link khi app được mở từ QR code hoặc URL scheme
        HandleAppStart(Environment.GetCommandLineArgs());
	}

    private void HandleAppStart(string[] args)
    {
        // Command line args chứa URL khi app được mở từ deep link
        foreach (var arg in args)
        {
            if (arg.StartsWith("foodmarketnarrator://", StringComparison.OrdinalIgnoreCase))
            {
                // Deep link format: foodmarketnarrator://open
                // App sẽ mở MainPage mặc định
                Debug.WriteLine($"[App] Deep link received: {arg}");
                _qrAccessService.ApplyDeepLink(arg);
                EnsureQrAccessGuardLoopState();
                break;
            }
        }
    }

    private void OnDeepLinkReceived(string deepLinkUrl)
    {
        _qrAccessService.ApplyDeepLink(deepLinkUrl);
        EnsureQrAccessGuardLoopState();
        Debug.WriteLine($"[App] Deep link received via dispatcher: {deepLinkUrl}");
    }

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

    protected override void OnStart()
    {
        base.OnStart();

        // Không chặn luồng startup: warm-up data chạy nền để lần mở trang đầu mượt hơn.
        StartWarmupInBackground();
        _ = Task.Run(() => _audioLibraryService.InitializeOnStartupAsync());
        _locationLogSyncService.Start();
        _ = _locationService.StartTrackingAsync();
        EnsureQrAccessGuardLoopState();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _ = _locationLogSyncService.FlushNowAsync();
        // For true background tracking without Foreground Service, OS might kill this.
        // We'll leave it running to hope for the best if permission allows background (on Android).
        // If strict lifecycle management is needed, consider StopTracking() here.
    }

    private void EnsureQrAccessGuardLoopState()
    {
        if (!_qrAccessService.IsQrTimeRestricted)
        {
            _qrAccessGuardCts?.Cancel();
            _qrAccessGuardCts = null;
            return;
        }

        if (_qrAccessGuardCts != null && !_qrAccessGuardCts.IsCancellationRequested)
        {
            return;
        }

        _qrAccessGuardCts = new CancellationTokenSource();
        var token = _qrAccessGuardCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var sessionId = _locationLogSyncService.CurrentSessionId;
                var allowed = await _qrAccessService.CanContinueNarrationAsync(sessionId, token);
                if (!allowed)
                {
                    await HandleQrAccessExpiredAsync();
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private async Task HandleQrAccessExpiredAsync()
    {
        Debug.WriteLine($"[App] QR access expired. Stop narration. reason={_qrAccessService.LastBlockReason}");

        try
        {
            await _locationLogSyncService.FlushNowAsync();
        }
        catch
        {
            // Ignore flush failure when handling QR expiry.
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            _narrationFlowService.StopNarration();

            var activePage = Shell.Current?.CurrentPage;
            if (activePage == null && Current?.Windows.Count > 0)
            {
                activePage = Current.Windows[0].Page;
            }

            if (activePage != null)
            {
                await activePage.DisplayAlertAsync(
                    "QR hết hạn",
                    "Mã QR đã hết thời gian. Hệ thống đã dừng thuyết minh, vui lòng quét lại QR để tiếp tục.",
                    "Đóng");
            }
        });
    }

    private void StartWarmupInBackground()
    {
        if (_warmupStarted)
        {
            return;
        }

        _warmupStarted = true;
        _ = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await Task.Delay(AppSettings.StartupWarmupDelayMs);

                // Console.WriteLine("[Perf][App] Warm-up started");
                await _languageService.GetAllLanguagesAsync();
                await _tourService.GetToursAsync();
                await _poiService.GetAllPOIsAsync();
                // Console.WriteLine($"[Perf][App] Warm-up finished in {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception)
            {
                // Console.WriteLine($"[Perf][App] Warm-up failed after {sw.ElapsedMilliseconds} ms: {ex.Message}");
            }
        });
    }
}



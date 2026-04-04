using food_market_narrator.Services;
using System.Diagnostics;

namespace food_market_narrator;

public partial class App : Application
{
    private readonly ILocationService _locationService;
    private readonly ILocationLogSyncService _locationLogSyncService;
    private readonly IPOIService _poiService;
    private readonly ILanguageService _languageService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IQrAccessService _qrAccessService;
    private bool _warmupStarted;
    private CancellationTokenSource? _qrAccessGuardCts;

    public App(
        ILocationService locationService,
        ILocationLogSyncService locationLogSyncService,
        IPOIService poiService,
        ILanguageService languageService,
        IAudioLibraryService audioLibraryService,
        IQrAccessService qrAccessService)
	{
		InitializeComponent();
        _locationService = locationService;
		_locationLogSyncService = locationLogSyncService;
		_poiService = poiService;
		_languageService = languageService;
        _audioLibraryService = audioLibraryService;
        _qrAccessService = qrAccessService;

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
        Debug.WriteLine($"[App] QR access expired. Closing app. reason={_qrAccessService.LastBlockReason}");

        try
        {
            await _locationLogSyncService.FlushNowAsync();
        }
        catch
        {
            // Ignore flush failure when force-closing app.
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
#if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            activity?.FinishAffinity();
            Java.Lang.JavaSystem.Exit(0);
#else
            Process.GetCurrentProcess().Kill();
#endif
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
                // Console.WriteLine("[Perf][App] Warm-up started");
                await Task.WhenAll(
                    _poiService.GetAllPOIsAsync(),
                    _languageService.GetAllLanguagesAsync());
                // Console.WriteLine($"[Perf][App] Warm-up finished in {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception)
            {
                // Console.WriteLine($"[Perf][App] Warm-up failed after {sw.ElapsedMilliseconds} ms: {ex.Message}");
            }
        });
    }
}



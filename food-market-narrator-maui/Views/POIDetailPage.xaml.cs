using food_market_narrator.Services;
using food_market_narrator.Models;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

[QueryProperty(nameof(RestaurantId), "restaurantId")]
public partial class POIDetailPage : ContentPage
{
	private readonly IPOIService? _poiService;
	private readonly IAudioService? _audioService;
	private readonly ILanguageService? _languageService;
	private IDispatcherTimer? _progressTimer;
	private string _restaurantId = string.Empty;
	private const string PlayGlyph = "\uf04b";
	private const string StopGlyph = "\uf04c";

	public string RestaurantId
	{
		get => _restaurantId;
		set
		{
			_restaurantId = Uri.UnescapeDataString(value ?? string.Empty);
			_ = LoadPoiDetailAsync();
		}
	}

	public POIDetailPage()
	{
		InitializeComponent();
		var services = Application.Current?.Handler?.MauiContext?.Services;
		_poiService = services?.GetService<IPOIService>();
		_audioService = services?.GetService<IAudioService>();
		_languageService = services?.GetService<ILanguageService>();

		if (_audioService != null)
		{
			_audioService.PlaybackEnded += OnPlaybackEnded;
		}

		ResetAudioProgressUi();
	}

	private async Task LoadPoiDetailAsync()
	{
		if (_poiService is null || string.IsNullOrWhiteSpace(_restaurantId))
		{
			return;
		}

		var poi = await _poiService.GetPOIByIdAsync(_restaurantId);
		if (poi is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			BindingContext = poi;
			SyncAudioUiWithService();
		});
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SyncAudioUiWithService();
	}

	private async void OnPlayAudioTapped(object sender, EventArgs e)
	{
		if (_audioService is null)
		{
			return;
		}

		// Đang phát → tạm dừng
		if (_audioService.IsPlaying)
		{
			_audioService.Pause();
			SetPlayButtonState(false);
			StopProgressTimer();
			return;
		}

		// Đang tạm dừng → tiếp tục phát
		if (_audioService.IsPaused)
		{
			_audioService.Resume();
			SetPlayButtonState(true);
			StartProgressTimer();
			return;
		}

		// Chưa phát gì → bắt đầu phát mới
		if (BindingContext is not POI poi)
		{
			return;
		}

		var language = _languageService?.CurrentLanguage ?? "vi-VN";
		var audioUrl = poi.GetAudioUrl(language);

		if (string.IsNullOrWhiteSpace(audioUrl))
		{
			return;
		}

		ResetAudioProgressUi();
		await _audioService.PlaySound(language, audioUrl);
		SetPlayButtonState(_audioService.IsPlaying);
		StartProgressTimer();
	}

	private void StartProgressTimer()
	{
		StopProgressTimer();

		_progressTimer = Dispatcher.CreateTimer();
		_progressTimer.Interval = TimeSpan.FromMilliseconds(200);
		_progressTimer.Tick += (_, _) => UpdateAudioProgressUi();
		_progressTimer.Start();
	}

	private void StopProgressTimer()
	{
		if (_progressTimer == null)
		{
			return;
		}

		_progressTimer.Stop();
		_progressTimer = null;
	}

	private void UpdateAudioProgressUi()
	{
		if (_audioService is null)
		{
			return;
		}

		var current = _audioService.CurrentPosition;
		var duration = _audioService.Duration;

		CurrentTimeLabel.Text = FormatTime(current);
		TotalTimeLabel.Text = FormatTime(duration);

		if (duration > TimeSpan.Zero)
		{
			AudioProgressBar.Progress = Math.Clamp(current.TotalSeconds / duration.TotalSeconds, 0, 1);
		}

		if (!_audioService.IsPlaying)
		{
			SetPlayButtonState(false);
			StopProgressTimer();
		}
	}

	private void ResetAudioProgressUi()
	{
		CurrentTimeLabel.Text = "00:00";
		TotalTimeLabel.Text = "00:00";
		AudioProgressBar.Progress = 0;
		SetPlayButtonState(false);
	}

	private void SyncAudioUiWithService()
	{
		if (_audioService is null)
		{
			ResetAudioProgressUi();
			return;
		}

		if (_audioService.IsPlaying)
		{
			SetPlayButtonState(true);
			UpdateAudioProgressUi();
			StartProgressTimer();
			return;
		}

		StopProgressTimer();

		if (_audioService.IsPaused)
		{
			SetPlayButtonState(false);
			UpdateAudioProgressUi();
			return;
		}

		ResetAudioProgressUi();
	}

	private void SetPlayButtonState(bool isPlaying)
	{
		PlayIconLabel.Text = isPlaying ? StopGlyph : PlayGlyph;
		PlayIconLabel.Margin = isPlaying ? new Thickness(0) : new Thickness(2, 0, 0, 0);
	}

	private static string FormatTime(TimeSpan time)
	{
		if (time < TimeSpan.Zero)
		{
			return "00:00";
		}

		if (time.TotalHours >= 1)
		{
			return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
		}

		return $"{time.Minutes:00}:{time.Seconds:00}";
	}

	private void OnPlaybackEnded(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			UpdateAudioProgressUi();
			SetPlayButtonState(false);
		});
	}

	protected override void OnDisappearing()
	{
		StopProgressTimer();
		base.OnDisappearing();
	}

	protected override void OnHandlerChanging(HandlerChangingEventArgs args)
	{
		if (args.NewHandler == null && _audioService != null)
		{
			_audioService.PlaybackEnded -= OnPlaybackEnded;
		}

		base.OnHandlerChanging(args);
	}

	// Hàm xử lý khi nhấn vào nút back để quay lại trang chính
	private async void OnBackButtonTapped(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}
using food_market_narrator.Services;
using food_market_narrator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace food_market_narrator.Views;

[QueryProperty(nameof(RestaurantId), "restaurantId")]
public partial class POIDetailPage : ContentPage
{
	private readonly IPOIService? _poiService;
	private readonly IAudioService? _audioService;
	private readonly ILanguageService? _languageService;
	private readonly IFavoriteService? _favoriteService;
	private readonly IHistoryService? _historyService;
	private IDispatcherTimer? _progressTimer;
	private string _restaurantId = string.Empty;
	private POI? _currentPoi;
	private const string PlayGlyph = "\uf04b";
	private const string StopGlyph = "\uf04c";
	private const string HeartSolid = "\uf004"; // filled heart
	private const string HeartRegular = "\uf08a"; // outline heart

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
		_favoriteService = services?.GetService<IFavoriteService>();
		_historyService = services?.GetService<IHistoryService>();

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

		// Load dishes từ API
		var dishes = await _poiService.GetDishesByRestaurantIdAsync(_restaurantId);
		if (dishes != null && dishes.Count > 0)
		{
			poi.Dishes = dishes;
		}

		// Lưu vào lịch sử
		_historyService?.AddToHistory(_restaurantId);

		_currentPoi = poi;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			BindingContext = poi;
			SyncAudioUiWithService();
			UpdateFavoriteIcon();
		});
	}

	private void UpdateFavoriteIcon()
	{
		if (_currentPoi == null || FavoriteIcon == null)
			return;

		var isFavorite = _favoriteService?.IsFavorite(_currentPoi.restaurantId) ?? false;
		// FavoriteIcon.Text = isFavorite ? HeartSolid : HeartRegular;
		FavoriteIcon.TextColor = isFavorite
        ? Colors.Red
        : Color.FromArgb("#ffffff");
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

		if (!TryGetCurrentPoiAudio(out var language, out var audioUrl))
		{
			return;
		}

		var isThisTrack = _audioService.IsCurrentTrack(language, audioUrl);

		// Đang phát đúng track hiện tại của trang -> tạm dừng
		if (isThisTrack && _audioService.IsPlaying)
		{
			_audioService.Pause();
			SetPlayButtonState(false);
			StopProgressTimer();
			return;
		}

		// Đang tạm dừng đúng track hiện tại của trang -> tiếp tục phát
		if (isThisTrack && _audioService.IsPaused)
		{
			_audioService.Resume();
			SetPlayButtonState(true);
			StartProgressTimer();
			return;
		}

		// Track khác đang phát hoặc chưa phát gì -> phát track của POI hiện tại
		ResetAudioProgressUi();
		await _audioService.PlaySound(language, audioUrl);
		SetPlayButtonState(_audioService.IsPlaying);

		if (_audioService.IsPlaying)
		{
			StartProgressTimer();
		}
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

		if (!TryGetCurrentPoiAudio(out var language, out var audioUrl))
		{
			ResetAudioProgressUi();
			return;
		}

		var isThisTrack = _audioService.IsCurrentTrack(language, audioUrl);
		if (!isThisTrack)
		{
			StopProgressTimer();
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

	private bool TryGetCurrentPoiAudio(out string language, out string audioUrl)
	{
		language = _languageService?.CurrentLanguage ?? "vi-VN";
		audioUrl = string.Empty;

		if (BindingContext is not POI poi)
		{
			return false;
		}

		var resolvedAudio = poi.GetAudioUrl(language);
		if (string.IsNullOrWhiteSpace(resolvedAudio))
		{
			return false;
		}

		audioUrl = resolvedAudio;
		return true;
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

	// Hàm xử lý khi nhấn nút Đường đi
	private async void OnGetDirectionClicked(object sender, EventArgs e)
	{
		if (BindingContext is not POI poi)
		{
			return;
		}

		// Mở Google Maps hoặc ứng dụng bản đồ mặc định
		var destination = $"{poi.Latitude},{poi.Longitude}";
		var label = Uri.EscapeDataString(poi.Name ?? "Điểm đến");

		try
		{
			// Thử mở Google Maps trước
			var googleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={destination}&destination_place_id={poi.restaurantId}";
			await Launcher.OpenAsync(googleMapsUrl);
		}
		catch (Exception)
		{
			// Nếu không mở được, thử mở ứng dụng bản đồ mặc định
			try
			{
				var mapsUrl = $"geo:{destination}?q={destination}({label})";
				await Launcher.OpenAsync(mapsUrl);
			}
			catch
			{
				// Hiển thị thông báo lỗi nếu không mở được bất kỳ ứng dụng nào
				await DisplayAlert("Lỗi", "Không thể mở ứng dụng bản đồ", "OK");
			}
		}
	}

	// Hàm xử lý khi nhấn nút Gọi điện
	private async void OnCallNowClicked(object sender, EventArgs e)
	{
		if (BindingContext is not POI poi)
		{
			return;
		}

		// Kiểm tra có số điện thoại không
		if (string.IsNullOrWhiteSpace(poi.Phone))
		{
			await DisplayAlert("Thông báo", "Quán chưa có số điện thoại", "OK");
			return;
		}

		// Mở ứng dụng gọi điện
		try
		{
			var phoneUrl = $"tel:{poi.Phone}";
			await Launcher.OpenAsync(phoneUrl);
		}
		catch (Exception)
		{
			await DisplayAlert("Lỗi", "Không thể mở ứng dụng gọi điện", "OK");
		}
	}

	// Hàm xử lý khi nhấn nút Yêu thích
	private void OnFavoriteTapped(object sender, EventArgs e)
	{
		if (_currentPoi == null)
			return;

		var restaurantId = _currentPoi.restaurantId;
		var isFavorite = _favoriteService?.IsFavorite(restaurantId) ?? false;

		if (isFavorite)
		{
			_favoriteService?.RemoveFavorite(restaurantId);
		}
		else
		{
			_favoriteService?.AddFavorite(restaurantId);
		}

		// Cập nhật icon
		UpdateFavoriteIcon();
	}
}
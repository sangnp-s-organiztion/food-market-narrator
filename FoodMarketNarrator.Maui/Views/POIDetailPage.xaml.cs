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
	private readonly IAudioLogSyncService? _audioLogSyncService;
	private IDispatcherTimer? _progressTimer;
	private string _restaurantId = string.Empty;
	private POI? _currentPoi;
	private DateTime? _playbackStartUtc;
	private int _playbackAudioId;
	private string _playbackRestaurantId = string.Empty;
	private string _lastAppliedLanguageCode = string.Empty;
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
		_audioLogSyncService = services?.GetService<IAudioLogSyncService>();

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

		_currentPoi = poi;
		var requestRestaurantId = _restaurantId;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			BindingContext = poi;
			_lastAppliedLanguageCode = _languageService?.CurrentLanguage ?? "vi-VN";
			SyncAudioUiWithService();
			UpdateFavoriteIcon();
		});

		_ = LoadDishesInBackgroundAsync(poi, requestRestaurantId);
	}

	private async Task LoadDishesInBackgroundAsync(POI poi, string requestRestaurantId)
	{
		if (_poiService is null || string.IsNullOrWhiteSpace(requestRestaurantId))
		{
			return;
		}

		try
		{
			var dishes = await _poiService.GetDishesByRestaurantIdAsync(requestRestaurantId);
			if (dishes == null || dishes.Count == 0)
			{
				return;
			}

			if (!string.Equals(_restaurantId, requestRestaurantId, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			poi.Dishes = dishes;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!string.Equals(_restaurantId, requestRestaurantId, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				BindingContext = poi;
			});
		}
		catch
		{
			// Bỏ qua lỗi dishes để không chặn UI chi tiết POI.
		}
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
		var currentLanguage = _languageService?.CurrentLanguage ?? "vi-VN";
		if (!string.Equals(_lastAppliedLanguageCode, currentLanguage, StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(_restaurantId))
		{
			_ = LoadPoiDetailAsync();
		}

		SyncAudioUiWithService();
		UpdateNarrationActionAvailability();
	}

	private async void OnPlayAudioTapped(object sender, EventArgs e)
	{
		if (_audioService is null)
		{
			return;
		}

		if (!TryGetCurrentPoiAudio(out var language, out var audioUrl, out var audioId))
		{
			return;
		}

		var isThisTrack = _audioService.IsCurrentTrack(audioId);

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
			AddCurrentPoiToHistoryIfPlaying();
			return;
		}

		// Track khác đang phát hoặc chưa phát gì -> phát track của POI hiện tại
		ResetAudioProgressUi();
		_playbackStartUtc = null;
		_playbackAudioId = audioId;
		_playbackRestaurantId = _currentPoi?.restaurantId ?? string.Empty;
		await _audioService.PlaySound(audioId);

		if (await WaitForPlaybackStartAsync())
		{
			_playbackStartUtc = DateTime.UtcNow;
		}
		SetPlayButtonState(_audioService.IsPlaying);

		if (_audioService.IsPlaying)
		{
			StartProgressTimer();
			AddCurrentPoiToHistoryIfPlaying();
		}
		else
		{
			ClearPlaybackContext();
		}
	}

	// hàm này sẽ chờ trong khoảng thời gian nhất định để xác nhận xem audio có thực sự bắt đầu phát hay không, vì đôi khi có thể có độ trễ giữa lệnh play và khi trạng thái IsPlaying được cập nhật
	private async Task<bool> WaitForPlaybackStartAsync(int timeoutMs = 2000)
	{
		if (_audioService == null)
		{
			return false;
		}

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

	private void AddCurrentPoiToHistoryIfPlaying()
	{
		if (_audioService is null || !_audioService.IsPlaying)
		{
			return;
		}

		if (_currentPoi == null || string.IsNullOrWhiteSpace(_currentPoi.restaurantId))
		{
			return;
		}

		_historyService?.AddToHistory(_currentPoi.restaurantId);
	}

	// Hàm xử lý khi người dùng kéo thanh progress để tua audio
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
		UpdateNarrationActionAvailability();

		if (_audioService is null)
		{
			ResetAudioProgressUi();
			return;
		}

		if (!TryGetCurrentPoiAudio(out var language, out var audioUrl, out var audioId))
		{
			ResetAudioProgressUi();
			return;
		}

		var isThisTrack = _audioService.IsCurrentTrack(audioId);
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

	// Hàm này sẽ cố gắng lấy thông tin audio của POI hiện tại dựa trên ngôn ngữ, nếu có audio phù hợp với ngôn ngữ hiện tại thì sẽ trả về, nếu không sẽ trả về audio mặc định (nếu có)
	private bool TryGetCurrentPoiAudio(out string language, out string audioUrl, out int audioId)
	{
		language = _languageService?.CurrentLanguage ?? "vi-VN";
		audioUrl = string.Empty;
		audioId = 0;

		if (BindingContext is not POI poi)
		{
			return false;
		}

		var selectedAudio = ResolveSelectedAudio(poi, language);
		if (selectedAudio == null || string.IsNullOrWhiteSpace(selectedAudio.AudioUrl))
		{
			return false;
		}

		audioUrl = selectedAudio.AudioUrl;
		audioId = selectedAudio.AudioId;
		return true;
	}

	// Hàm này sẽ cập nhật trạng thái của nút play dựa trên việc audio có đang phát hay không
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

	// Hàm này sẽ cập nhật trạng thái của các hành động liên quan đến narration (hiện tại là nút play) dựa trên việc có audio nào phù hợp để phát hay không
	private void UpdateNarrationActionAvailability()
	{
		if (PlayAudioButton == null)
		{
			return;
		}

		PlayAudioButton.IsEnabled = true;
		PlayAudioButton.Opacity = 1;
	}

	// Hàm xử lý khi audio kết thúc, sẽ ghi nhận lại thông tin playback vào log nếu có đủ thông tin, sau đó reset trạng thái UI và ngữ cảnh liên quan đến playback
	private void OnPlaybackEnded(object? sender, EventArgs e)
	{
		LogPlaybackIfPossible(DateTime.UtcNow);

		ClearPlaybackContext();

		MainThread.BeginInvokeOnMainThread(() =>
		{
			UpdateAudioProgressUi();
			SetPlayButtonState(false);
		});
	}

	protected override void OnDisappearing()
	{
		StopProgressTimer();
		LogPlaybackIfPossible(DateTime.UtcNow);
		ClearPlaybackContext();
		base.OnDisappearing();
	}
	 
	// Hàm này sẽ ghi nhận lại thông tin playback vào log nếu có đủ thông tin, bao gồm thời gian bắt đầu, thời gian kết thúc, độ dài đã phát so với tổng độ dài của track, v.v. Thông tin này sẽ được gửi đến service để đồng bộ với backend
	private void LogPlaybackIfPossible(DateTime endedAtUtc)
	{
		if (_audioLogSyncService == null
			|| !_playbackStartUtc.HasValue
			|| _playbackAudioId <= 0
			|| string.IsNullOrWhiteSpace(_playbackRestaurantId))
		{
			return;
		}

		var startedAtUtc = _playbackStartUtc.Value;
		if (endedAtUtc < startedAtUtc)
		{
			endedAtUtc = startedAtUtc;
		}

		var restaurantId = _playbackRestaurantId;
		var audioId = _playbackAudioId;
		var playedDurationSeconds = _audioService != null
			? (int)Math.Round(_audioService.CurrentPosition.TotalSeconds)
			: 0;
		var trackDurationSeconds = _audioService != null
			? (int)Math.Round(_audioService.Duration.TotalSeconds)
			: 0;
		_ = Task.Run(() => _audioLogSyncService.LogPlaybackAsync(
			restaurantId,
			audioId,
			startedAtUtc,
			endedAtUtc,
			playedDurationSeconds,
			trackDurationSeconds));
	}

	// Hàm này sẽ xóa bỏ ngữ cảnh liên quan đến playback hiện tại, bao gồm reset các biến lưu trữ thông tin về audio đang phát. Hàm này sẽ được gọi sau khi đã ghi nhận lại thông tin playback vào log (nếu có thể) để đảm bảo rằng ngữ cảnh chỉ được xóa sau khi đã lưu lại thông tin cần thiết
	private void ClearPlaybackContext()
	{
		_playbackStartUtc = null;
		_playbackAudioId = 0;
		_playbackRestaurantId = string.Empty;
	}

	// Hàm này sẽ cố gắng lấy thông tin audio của POI hiện tại dựa trên ngôn ngữ, nếu có audio phù hợp với ngôn ngữ hiện tại thì sẽ trả về, nếu không sẽ trả về audio mặc định (nếu có)	
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


	// Hàm này để dọn dẹp event khi page bị tháo khỏi UI handler.
	protected override void OnHandlerChanging(HandlerChangingEventArgs args)
	{
		if (args.NewHandler == null && _audioService != null)
		{
			_audioService.PlaybackEnded -= OnPlaybackEnded;
		}

		base.OnHandlerChanging(args);
	}

	// Hàm xử lý khi nhấn vào nút back để quay lại trang trước đó
	private async void OnBackButtonTapped(object sender, EventArgs e)
	{
		var navigation = Shell.Current?.Navigation;
		if (navigation?.NavigationStack != null && navigation.NavigationStack.Count > 1)
		{
			await navigation.PopAsync(false);
			return;
		}

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
		var destination = $"{poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
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
				await DisplayAlert("Lỗi", "Không thể mở ứng dụng bản đồ", "Đóng");
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
			await DisplayAlert("Thông báo", "Quán chưa có số điện thoại", "Đóng");
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
			await DisplayAlert("Lỗi", "Không thể mở ứng dụng gọi điện", "Đóng");
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

	private async void OnShareTapped(object sender, EventArgs e)
	{
		if (BindingContext is not POI poi)
		{
			return;
		}

		try
		{
			var name = string.IsNullOrWhiteSpace(poi.Name) ? "Quán ăn" : poi.Name;
			var address = string.IsNullOrWhiteSpace(poi.AddressDisplay)
				? "Đang cập nhật địa chỉ"
				: poi.AddressDisplay;
			var mapsUrl = $"https://www.google.com/maps/search/?api=1&query={poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

			var shareText =
				$"{name}\n" +
				$"Địa chỉ: {address}\n" +
				$"Bản đồ: {mapsUrl}";

			await Share.RequestAsync(new ShareTextRequest
			{
				Title = $"Chia sẻ {name}",
				Text = shareText,
				Uri = mapsUrl
			});
		}
		catch (Exception)
		{
			await DisplayAlert("Lỗi", "Không thể chia sẻ thông tin quán lúc này", "Đóng");
		}
	}
}
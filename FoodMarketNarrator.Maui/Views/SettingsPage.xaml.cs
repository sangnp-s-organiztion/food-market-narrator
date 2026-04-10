using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;
using food_market_narrator.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Generic;
using food_market_narrator.Helpers;
using food_market_narrator.Settings;
using IOPath = System.IO.Path;

namespace food_market_narrator.Views;

public partial class SettingsPage : ContentPage
{
    private const string OfflineCacheFolderName = "offline_cache";
    private const string ImageCacheFolderName = "image_cache";

    private readonly IAudioService? _audioService;
    private readonly ILanguageService? _languageService;
    private readonly IFavoriteService? _favoriteService;
    private readonly IHistoryService? _historyService;
    private readonly NarrationFlowService? _narrationFlowService;
    private readonly ILocationService? _locationService;

    private readonly Dictionary<string, Border> _languageOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _languageChecks = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLanguageOptionsLoaded;
    private bool _isUpdatingBackgroundToggle;
    private VerticalStackLayout? _languageOptionsContainer;
    private Grid? _languagePopupOverlay;
    private StorageUsageSummary _lastStorageUsage = new();

    public SettingsPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _audioService = services?.GetService<IAudioService>();
        _languageService = services?.GetService<ILanguageService>();
        _favoriteService = services?.GetService<IFavoriteService>();
        _historyService = services?.GetService<IHistoryService>();
        _narrationFlowService = services?.GetService<NarrationFlowService>();
        _locationService = services?.GetService<ILocationService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        // Load current language
        if (_languageService != null)
        {
            CurrentLanguageLabel.Text = GetLanguageDisplayName(_languageService.CurrentLanguage);
        }

        await LoadOfflineDataUsageAsync();

        await UpdateBackgroundPermissionStatusAsync();
    }

    private async Task LoadOfflineDataUsageAsync()
    {
        try
        {
            _lastStorageUsage = await ReadStorageUsageAsync();
            StorageTotalSizeLabel.Text = FormatBytesCompact(_lastStorageUsage.TotalBytes);

            StorageMapLegendLabel.Text = $"Bản đồ {FormatBytesCompact(_lastStorageUsage.MapBytes)}";
            StorageImageLegendLabel.Text = $"Ảnh {FormatBytesCompact(_lastStorageUsage.ImageBytes)}";
            StorageOtherLegendLabel.Text = $"Khác {FormatBytesCompact(_lastStorageUsage.OtherBytes)}";

            ApplyStorageBarSegments(_lastStorageUsage);
        }
        catch
        {
            _lastStorageUsage = new StorageUsageSummary();
            StorageTotalSizeLabel.Text = "0 B";
            StorageMapLegendLabel.Text = "Bản đồ 0 B";
            StorageImageLegendLabel.Text = "Ảnh 0 B";
            StorageOtherLegendLabel.Text = "Khác 0 B";
            ApplyStorageBarSegments(_lastStorageUsage);
        }
    }

    private async Task<StorageUsageSummary> ReadStorageUsageAsync()
    {
        var appData = FileSystem.AppDataDirectory;

        var offlineCacheRoot = IOPath.Combine(appData, OfflineCacheFolderName);
        var imageCacheRoot = IOPath.Combine(appData, ImageCacheFolderName);
        var mapCacheRoot = AppSettings.MapTileCacheDirectory;

        var poiFilePath = IOPath.Combine(offlineCacheRoot, "pois.json");
        var languageFilePath = IOPath.Combine(offlineCacheRoot, "languages.json");
        var dishesDirPath = IOPath.Combine(offlineCacheRoot, "dishes");

        var audioBytes = _audioService != null
            ? await _audioService.GetCachedAudioSizeBytesAsync()
            : GetDirectorySizeSafe(IOPath.Combine(appData, "audio_cache"));

        var poiBytes = GetFileSizeSafe(poiFilePath);
        var languageBytes = GetFileSizeSafe(languageFilePath);
        var dishesBytes = GetDirectorySizeSafe(dishesDirPath);
        var imageBytes = GetDirectorySizeSafe(imageCacheRoot);
        var mapBytes = GetDirectorySizeSafe(mapCacheRoot);

        return new StorageUsageSummary
        {
            AudioBytes = audioBytes,
            PoiBytes = poiBytes,
            DishesBytes = dishesBytes,
            LanguageBytes = languageBytes,
            ImageBytes = imageBytes,
            MapBytes = mapBytes,
            ImageFileCount = GetFileCountSafe(imageCacheRoot),
            MapFileCount = GetFileCountSafe(mapCacheRoot),
            DishesFileCount = GetFileCountSafe(dishesDirPath)
        };
    }

    private void ApplyStorageBarSegments(StorageUsageSummary usage)
    {
        var total = usage.TotalBytes;
        if (total <= 0)
        {
            MapSegmentColumn.Width = new GridLength(0, GridUnitType.Star);
            ImageSegmentColumn.Width = new GridLength(0, GridUnitType.Star);
            OtherSegmentColumn.Width = new GridLength(0, GridUnitType.Star);
            UnusedSegmentColumn.Width = new GridLength(1, GridUnitType.Star);
            return;
        }

        var map = usage.MapBytes;
        var image = usage.ImageBytes;
        var other = usage.OtherBytes;

        var mapShare = map / (double)total;
        var imageShare = image / (double)total;
        var otherShare = other / (double)total;

        mapShare = ApplyMinVisibleShare(mapShare, map);
        imageShare = ApplyMinVisibleShare(imageShare, image);
        otherShare = ApplyMinVisibleShare(otherShare, other);

        var sum = mapShare + imageShare + otherShare;
        if (sum <= 0)
        {
            UnusedSegmentColumn.Width = new GridLength(1, GridUnitType.Star);
            return;
        }

        var scale = 1d / sum;
        mapShare *= scale;
        imageShare *= scale;
        otherShare *= scale;

        MapSegmentColumn.Width = new GridLength(mapShare, GridUnitType.Star);
        ImageSegmentColumn.Width = new GridLength(imageShare, GridUnitType.Star);
        OtherSegmentColumn.Width = new GridLength(otherShare, GridUnitType.Star);
        UnusedSegmentColumn.Width = new GridLength(0, GridUnitType.Star);
    }

    private static double ApplyMinVisibleShare(double share, long bytes)
    {
        if (bytes <= 0)
        {
            return 0;
        }

        return Math.Max(share, 0.04d);
    }

    private static long GetFileSizeSafe(string filePath)
    {
        try
        {
            return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static long GetDirectorySizeSafe(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return 0;
            }

            return Directory
                .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Sum(path => GetFileSizeSafe(path));
        }
        catch
        {
            return 0;
        }
    }

    private static int GetFileCountSafe(string directoryPath)
    {
        try
        {
            return Directory.Exists(directoryPath)
                ? Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Count()
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task UpdateBackgroundPermissionStatusAsync()
    {
#if ANDROID
        if (_locationService == null)
        {
            BackgroundPermissionStatusLabel.Text = "Không thể kiểm tra quyền";
            _isUpdatingBackgroundToggle = true;
            BackgroundPermissionSwitch.IsToggled = false;
            _isUpdatingBackgroundToggle = false;
            BackgroundPermissionSwitch.IsEnabled = false;
            return;
        }

        var granted = await _locationService.HasBackgroundLocationPermissionAsync();
        BackgroundPermissionStatusLabel.Text = granted
            ? "Đã cấp quyền vị trí nền"
            : "Chưa cấp quyền vị trí nền";

        _isUpdatingBackgroundToggle = true;
        BackgroundPermissionSwitch.IsToggled = granted;
        _isUpdatingBackgroundToggle = false;
        BackgroundPermissionSwitch.IsEnabled = true;
#else
        BackgroundPermissionStatusLabel.Text = "Thiết bị này không yêu cầu quyền vị trí nền";
        _isUpdatingBackgroundToggle = true;
        BackgroundPermissionSwitch.IsToggled = false;
        _isUpdatingBackgroundToggle = false;
        BackgroundPermissionSwitch.IsEnabled = false;
#endif
    }

    private string GetLanguageDisplayName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "vi-vn" => "Tiếng Việt",
            // Redundant alternatives were removed because input is already normalized by ToLowerInvariant().
            // "en-us" or "en-US" => "Tiếng Anh",
            "en-us" => "Tiếng Anh",
            // "zh-cn" or "zh-CN" => "中文",
            "zh-cn" => "中文",
            // "ko-kr" or "ko-KR" => "한국어",
            "ko-kr" => "한국어",
            // "ja-jp" or "ja-JP" => "日本語",
            "ja-jp" => "日本語",
            _ => code
        };
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        var kb = bytes / 1024d;
        if (kb < 1024)
            return $"{kb:F1} KB";

        var mb = kb / 1024d;
        return $"{mb:F1} MB";
    }

    private static string FormatBytesCompact(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        var kb = bytes / 1024d;
        if (kb < 1024)
            return $"{kb:F1} KB";

        var mb = kb / 1024d;
        if (mb < 1024)
            return $"{mb:F1} MB";

        var gb = mb / 1024d;
        return $"{gb:F1} GB";
    }

    private async void OnLanguageTapped(object sender, EventArgs e)
    {
        // Tạo và hiển thị popup chọn ngôn ngữ
        await ShowLanguagePopupAsync();
    }

    private async Task ShowLanguagePopupAsync()
    {
        // Tạo overlay
        _languagePopupOverlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#80000000"),
            InputTransparent = false
        };

        // Tạo nội dung popup
        var popupContent = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Padding = new Thickness(20, 12, 20, 24),
            VerticalOptions = LayoutOptions.End
        };

        var contentStack = new VerticalStackLayout { Spacing = 16 };

        // Title
        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var titleLabel = new Label
        {
            Text = "Chọn ngôn ngữ thuyết minh",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A1A"),
            VerticalOptions = LayoutOptions.Center
        };
        titleGrid.Add(titleLabel, 0, 0);

        var closeButton = new Border
        {
            BackgroundColor = Color.FromArgb("#F1EDE9"),
            HeightRequest = 32,
            WidthRequest = 32,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 }
        };
        var closeLabel = new Label
        {
            Text = "\uF00D",
            FontFamily = "FASolid",
            FontSize = 14,
            TextColor = Color.FromArgb("#7D746D"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        closeButton.Content = closeLabel;
        closeButton.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await HideLanguagePopupAsync())
        });
        titleGrid.Add(closeButton, 1, 0);
        contentStack.Add(titleGrid);

        // Ngôn ngữ options container
        _languageOptionsContainer = new VerticalStackLayout { Spacing = 12 };
        contentStack.Add(_languageOptionsContainer);

        popupContent.Content = contentStack;

        // Stack chính
        var mainStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.End };
        mainStack.Add(popupContent);

        _languagePopupOverlay.Add(mainStack, 0, 1);

        // Thêm vào page
        if (Content is Grid mainGrid)
        {
            Grid.SetRowSpan(_languagePopupOverlay, 3);   // phủ toàn bộ 3 row
            mainGrid.Children.Add(_languagePopupOverlay);

            await BuildLanguageOptionsAsync();
            await _languagePopupOverlay.FadeTo(1, 180);
        }
    }

    private async Task HideLanguagePopupAsync()
    {
        if (_languagePopupOverlay != null)
        {
            await _languagePopupOverlay.FadeTo(0, 140);
            if (Content is Grid mainGrid)
            {
                mainGrid.Children.Remove(_languagePopupOverlay);
            }
            _languagePopupOverlay = null;
        }
    }

    private async Task BuildLanguageOptionsAsync()
    {
        if (_languageOptionsContainer == null || _languageService == null)
            return;

        // if (_isLanguageOptionsLoaded)
        //     return;

        var languages = await _languageService.GetAllLanguagesAsync();
        if (languages.Count == 0)
        {
            languages = new List<LanguageModel>
            {
                new() { LanguageCode = _languageService.CurrentLanguage, LanguageName = _languageService.CurrentLanguage }
            };
        }

        _languageOptionsContainer.Children.Clear();
        _languageOptions.Clear();
        _languageChecks.Clear();

        foreach (var language in languages)
        {
            if (string.IsNullOrWhiteSpace(language.LanguageCode))
                continue;

            var optionBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#F1EDE9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(14, 12)
            };

            optionBorder.GestureRecognizers.Add(new TapGestureRecognizer
            {
                CommandParameter = language.LanguageCode,
                Command = new Command(async () => await OnLanguageOptionTappedAsync(language.LanguageCode))
            });

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };

            // Flag
            var flagLabel = new Label
            {
                Text = GetFlagByLanguageCode(language.LanguageCode),
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(flagLabel, 0, 0);

            // Language name
            var nameLabel = new Label
            {
                Text = language.LanguageName,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(nameLabel, 1, 0);

            // Checkmark
            var checkLabel = new Label
            {
                Text = "✓",
                FontSize = 16,
                TextColor = Color.FromArgb("#F48C06"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            row.Add(checkLabel, 2, 0);

            optionBorder.Content = row;

            _languageOptions[language.LanguageCode] = optionBorder;
            _languageChecks[language.LanguageCode] = checkLabel;
            _languageOptionsContainer.Children.Add(optionBorder);
        }

        _isLanguageOptionsLoaded = true;
        ApplyLanguageSelectionStyle(_languageService.CurrentLanguage);
    }

    private void ApplyLanguageSelectionStyle(string cultureCode)
    {
        foreach (var option in _languageOptions)
        {
            var isSelected = option.Key.Equals(cultureCode, StringComparison.OrdinalIgnoreCase);

            option.Value.BackgroundColor = isSelected
                ? Color.FromArgb("#F7F3EF")
                : Color.FromArgb("#F1EDE9");

            option.Value.StrokeThickness = isSelected ? 1.5 : 0;
            option.Value.Stroke = isSelected
                ? Color.FromArgb("#F48C06")
                : Colors.Transparent;

            if (_languageChecks.TryGetValue(option.Key, out var checkLabel))
            {
                checkLabel.IsVisible = isSelected;
            }
        }
    }

    private async Task OnLanguageOptionTappedAsync(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode) || _languageService == null)
            return;

        var wasNarrating = _narrationFlowService?.IsNarrating ?? false;

        ApplyLanguageSelectionStyle(cultureCode);
        await HideLanguagePopupAsync();

        Preferences.Set("language_selected", true);
        Preferences.Set("language", cultureCode);

        if (_languageService.CurrentLanguage != cultureCode)
        {
            _languageService.ChangeLanguage(cultureCode);
        }

        // Cập nhật label hiển thị
        CurrentLanguageLabel.Text = GetLanguageDisplayName(cultureCode);

        if (wasNarrating)
        {
            _narrationFlowService?.StartNarration();
        }
    }

    private string GetFlagByLanguageCode(string languageCode)
    {
        return languageCode.ToLowerInvariant() switch
        {
            "vi-vn" => "🇻🇳",
            "en-us" => "🇺🇸",
            "zh-cn" => "🇨🇳",
            "ko-kr" => "🇰🇷",
            "ja-jp" => "🇯🇵",
            _ => "🌐"
        };
    }

    private async void OnClearCacheClicked(object sender, EventArgs e)
    {
        if (_audioService == null)
            return;

        var confirm = await DisplayAlert(
            "Xóa cache",
            "Bạn có chắc muốn xóa toàn bộ audio đã tải về máy?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        await _audioService.ClearAudioCacheAsync();
        await LoadOfflineDataUsageAsync();

        await DisplayAlert("Hoàn tất", "Đã xóa bộ nhớ audio", "Đóng");
    }

    private async void OnStorageDetailsClicked(object sender, EventArgs e)
    {
        var usage = _lastStorageUsage;
        var message = string.Join(Environment.NewLine, new[]
        {
            $"Tổng dữ liệu: {FormatBytesCompact(usage.TotalBytes)}",
            $"Bản đồ: {FormatBytesCompact(usage.MapBytes)} ({usage.MapFileCount} file)",
            $"Ảnh: {FormatBytesCompact(usage.ImageBytes)} ({usage.ImageFileCount} file)",
            $"Audio: {FormatBytesCompact(usage.AudioBytes)}",
            $"POI: {FormatBytesCompact(usage.PoiBytes)}",
            $"Món ăn: {FormatBytesCompact(usage.DishesBytes)} ({usage.DishesFileCount} file)",
            $"Ngôn ngữ: {FormatBytesCompact(usage.LanguageBytes)}"
        });

        await DisplayAlert("Chi tiết bộ nhớ", message, "Đóng");
    }

    private async void OnClearAllDataClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Xóa toàn bộ dữ liệu",
            "Thao tác này sẽ xóa cache bản đồ, ảnh, audio và dữ liệu offline đã tải. Bạn có chắc chắn?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        if (_audioService != null)
        {
            await _audioService.ClearAudioCacheAsync();
        }

        DeleteDirectorySafe(IOPath.Combine(FileSystem.AppDataDirectory, OfflineCacheFolderName));
        DeleteDirectorySafe(IOPath.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName));
        DeleteDirectorySafe(AppSettings.MapTileCacheDirectory);

        await LoadOfflineDataUsageAsync();

        await DisplayAlert("Hoàn tất", "Đã xóa toàn bộ dữ liệu offline.", "Đóng");
    }

    private static void DeleteDirectorySafe(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Ignore delete failures to avoid breaking settings UI.
        }
    }

    private sealed class StorageUsageSummary
    {
        public long MapBytes { get; set; }
        public long ImageBytes { get; set; }
        public long AudioBytes { get; set; }
        public long PoiBytes { get; set; }
        public long DishesBytes { get; set; }
        public long LanguageBytes { get; set; }
        public int MapFileCount { get; set; }
        public int ImageFileCount { get; set; }
        public int DishesFileCount { get; set; }

        public long OtherBytes => AudioBytes + PoiBytes + DishesBytes + LanguageBytes;
        public long TotalBytes => MapBytes + ImageBytes + OtherBytes;
    }

    private async void OnClearHistoryClicked(object sender, EventArgs e)
    {
        if (_historyService == null)
            return;

        var confirm = await DisplayAlert(
            "Xóa lịch sử",
            "Bạn có chắc muốn xóa toàn bộ lịch sử đã nghe?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        _historyService.ClearHistory();

        await DisplayAlert("Hoàn tất", "Đã xóa lịch sử đã nghe", "Đóng");
    }

    private async void OnOpenHistoryTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HistoryPage));
    }

    private async void OnOpenTourBannerClicked(object sender, EventArgs e)
    {
        if (Shell.Current == null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("//TourPage");
        }
        catch
        {
            // Ignore navigation race errors to keep settings page stable.
        }
    }

    private async void OnClearFavoritesClicked(object sender, EventArgs e)
    {
        if (_favoriteService == null)
            return;

        var confirm = await DisplayAlert(
            "Xóa yêu thích",
            "Bạn có chắc muốn xóa toàn bộ quán yêu thích?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        var favorites = _favoriteService.GetFavorites();
        foreach (var id in favorites.ToList())
        {
            _favoriteService.RemoveFavorite(id);
        }

        await DisplayAlert("Hoàn tất", "Đã xóa tất cả yêu thích", "Đóng");
    }

    private async void OnBackgroundLocationToggled(object sender, ToggledEventArgs e)
    {
        if (_isUpdatingBackgroundToggle)
            return;

#if !ANDROID
        return;
#else
        if (_locationService == null)
        {
            _isUpdatingBackgroundToggle = true;
            BackgroundPermissionSwitch.IsToggled = false;
            _isUpdatingBackgroundToggle = false;
            await DisplayAlert("Thông báo", "Không thể yêu cầu quyền vị trí nền lúc này.", "Đóng");
            return;
        }

        if (e.Value)
        {
            var granted = await _locationService.RequestBackgroundLocationPermissionAsync();
            _isUpdatingBackgroundToggle = true;
            BackgroundPermissionSwitch.IsToggled = granted;
            _isUpdatingBackgroundToggle = false;
            await UpdateBackgroundPermissionStatusAsync();

            if (!granted)
            {
                await DisplayAlert("Thông báo", "Bạn chưa cấp quyền vị trí nền.", "Đóng");
            }

            return;
        }

        var hasPermission = await _locationService.HasBackgroundLocationPermissionAsync();
        if (!hasPermission)
        {
            await UpdateBackgroundPermissionStatusAsync();
            return;
        }

        _isUpdatingBackgroundToggle = true;
        BackgroundPermissionSwitch.IsToggled = true;
        _isUpdatingBackgroundToggle = false;

        var openSettings = await DisplayAlert(
            "Quyền vị trí nền",
            "Để tắt quyền vị trí nền, vui lòng vào Cài đặt hệ thống của ứng dụng.",
            "Mở cài đặt",
            "Để sau");

        if (openSettings)
        {
            AppInfo.ShowSettingsUI();
        }
#endif
    }
}

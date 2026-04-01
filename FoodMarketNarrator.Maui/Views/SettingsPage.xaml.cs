using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;
using food_market_narrator.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using food_market_narrator.Helpers;

namespace food_market_narrator.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IAudioService? _audioService;
    private readonly ILanguageService? _languageService;
    private readonly IFavoriteService? _favoriteService;
    private readonly IHistoryService? _historyService;
    private readonly NarrationFlowService? _narrationFlowService;
    private readonly ILocationService? _locationService;

    private readonly Dictionary<string, Border> _languageOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _languageChecks = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLanguageOptionsLoaded;
    private bool _isApplyingToggleState;
    private VerticalStackLayout? _languageOptionsContainer;
    private Grid? _languagePopupOverlay;

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
        if (_audioService != null)
        {
            _audioService.CacheSizeChanged -= OnCacheSizeChanged;
            _audioService.CacheSizeChanged += OnCacheSizeChanged;
        }

        await LoadSettingsAsync();
    }

    protected override void OnDisappearing()
    {
        if (_audioService != null)
        {
            _audioService.CacheSizeChanged -= OnCacheSizeChanged;
        }

        base.OnDisappearing();
    }

    private async Task LoadSettingsAsync()
    {
        // Load current language
        if (_languageService != null)
        {
            CurrentLanguageLabel.Text = GetLanguageDisplayName(_languageService.CurrentLanguage);
        }

        // Load cache size
        if (_audioService != null)
        {
            var cacheBytes = await _audioService.GetCachedAudioSizeBytesAsync();
            CacheSizeLabel.Text = FormatBytes(cacheBytes);
        }

        if (_locationService != null)
        {
            _isApplyingToggleState = true;
            BackgroundTrackingSwitch.IsToggled = _locationService.IsBackgroundTrackingModeEnabled;
            _isApplyingToggleState = false;
        }
    }

    private string GetLanguageDisplayName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "vi-vn" or "vi-vn" => "Tiếng Việt",
            "en-us" or "en-US" => "English",
            "zh-cn" or "zh-CN" => "中文",
            "ko-kr" or "ko-KR" => "한국어",
            "ja-jp" or "ja-JP" => "日本語",
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

        var currentSize = await _audioService.GetCachedAudioSizeBytesAsync();
        if (currentSize <= 0)
        {
            await DisplayAlert("Thông báo", "Hiện không có audio nào được tải.", "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Xóa cache",
            "Bạn có chắc muốn xóa toàn bộ audio đã tải về máy?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        await _audioService.ClearAudioCacheAsync();

        var newSize = await _audioService.GetCachedAudioSizeBytesAsync();
        CacheSizeLabel.Text = FormatBytes(newSize);

        await DisplayAlert("Hoàn tất", "Đã xóa bộ nhớ audio", "OK");
    }

    private async void OnClearHistoryClicked(object sender, EventArgs e)
    {
        if (_historyService == null)
            return;

        var confirm = await DisplayAlert(
            "Xóa lịch sử",
            "Bạn có chắc muốn xóa toàn bộ lịch sử đã xem?",
            "Xóa",
            "Hủy");

        if (!confirm)
            return;

        _historyService.ClearHistory();

        await DisplayAlert("Hoàn tất", "Đã xóa lịch sử xem", "OK");
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

        await DisplayAlert("Hoàn tất", "Đã xóa tất cả yêu thích", "OK");
    }

    private async void OnBackgroundTrackingToggled(object sender, ToggledEventArgs e)
    {
        if (_isApplyingToggleState || _locationService == null)
            return;

        var success = await _locationService.SetBackgroundTrackingModeAsync(e.Value);
        if (!success && e.Value)
        {
            _isApplyingToggleState = true;
            BackgroundTrackingSwitch.IsToggled = false;
            _isApplyingToggleState = false;

            await DisplayAlert(
                "Chưa bật được",
                "Bạn cần chọn 'Allow all the time' để bật theo dõi vị trí nền.",
                "OK");
        }
    }

    private void OnCacheSizeChanged(object? sender, long bytes)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CacheSizeLabel.Text = FormatBytes(bytes);
        });
    }
}

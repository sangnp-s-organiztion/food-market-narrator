using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IAudioService? _audioService;
    private readonly ILanguageService? _languageService;
    private readonly IFavoriteService? _favoriteService;
    private readonly IHistoryService? _historyService;

    public SettingsPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _audioService = services?.GetService<IAudioService>();
        _languageService = services?.GetService<ILanguageService>();
        _favoriteService = services?.GetService<IFavoriteService>();
        _historyService = services?.GetService<IHistoryService>();
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

        // Load cache size
        if (_audioService != null)
        {
            var cacheBytes = await _audioService.GetCachedAudioSizeBytesAsync();
            CacheSizeLabel.Text = FormatBytes(cacheBytes);
        }
    }

    private string GetLanguageDisplayName(string code)
    {
        return code switch
        {
            "vi-VN" => "Tiếng Việt",
            "en-US" => "English",
            "zh-CN" => "中文",
            "ko-KR" => "한국어",
            "ja-JP" => "日本語",
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
        // Mở popup chọn ngôn ngữ (tái sử dụng từ MainPage)
        if (Application.Current?.MainPage is Page mainPage)
        {
            await mainPage.DisplayAlert("Ngôn ngữ", "Vui lòng chọn ngôn ngữ từ trang chủ", "OK");
        }
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
}

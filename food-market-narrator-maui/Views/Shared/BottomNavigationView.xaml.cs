using food_market_narrator.Enums;
using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views.Shared;

public partial class BottomNavigationView : ContentView
{
    public static readonly BindableProperty ActiveTabProperty =
       BindableProperty.Create(
           nameof(ActiveTab),
           typeof(BottomTab),
           typeof(BottomNavigationView),
           BottomTab.None,
           propertyChanged: OnTabChanged);

    public BottomTab ActiveTab
    {
        get => (BottomTab)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public Color ActiveColor { get; set; } = Colors.Orange;
    public Color InactiveColor { get; set; } = Color.FromArgb("#8D6E63");

    public BottomNavigationView()
    {
        InitializeComponent();
    }

    private static void OnTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (BottomNavigationView)bindable;
        view.UpdateUI();
    }

    private void UpdateUI()
    {
        ResetColors();

        switch (ActiveTab)
        {
            case BottomTab.Home:
                SetActive(HomeIcon, HomeText);
                break;

            case BottomTab.Map:
                SetActive(MapIcon, MapText);
                break;

            case BottomTab.Favorite:
                SetActive(FavoriteIcon, FavoriteText);
                break;
        }
    }

    private void ResetColors()
    {
        // Set màu cho HomeIcon
        HomeIcon.TextColor = InactiveColor;
        HomeText.TextColor = InactiveColor;

        // Set màu cho MapIcon
        MapIcon.TextColor = InactiveColor;
        MapText.TextColor = InactiveColor;

        // Set màu cho FavoriteIcon
        FavoriteIcon.TextColor = InactiveColor;
        FavoriteText.TextColor = InactiveColor;
    }

    private void SetActive(Label icon, Label text)
    {
        icon.TextColor = ActiveColor;
        text.TextColor = ActiveColor;
    }


    // Mở bản đồ khi nhấn vào MapIcon hoặc MapText
    private async void OpenMap(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MapPage");
    }

    // Mở trang OpenMainPage khi nhấn vào HomeIcon hoặc HomeText
    private async void OpenMainPage(object sender, EventArgs e)
    {
        // Use absolute route to reset to the main tab/page
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OpenSettings(object sender, EventArgs e)
    {
        var page = Shell.Current?.CurrentPage;
        if (page == null)
        {
            return;
        }

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var audioService = services?.GetService<IAudioService>();
        if (audioService == null)
        {
            await page.DisplayAlertAsync("Cài đặt", "Không tìm thấy dịch vụ audio cache.", "Đóng");
            return;
        }

        var cacheBytes = await audioService.GetCachedAudioSizeBytesAsync();
        var cacheLabel = $"Xóa bộ nhớ audio đã tải ({FormatBytes(cacheBytes)})";

        var action = await page.DisplayActionSheetAsync(
            "Cài đặt",
            "Hủy",
            null,
            cacheLabel);

        if (action != cacheLabel)
        {
            return;
        }

        var confirm = await page.DisplayAlertAsync(
            "Xác nhận",
            "Bạn có chắc muốn xóa toàn bộ audio đã tải về máy?",
            "Xóa",
            "Hủy");

        if (!confirm)
        {
            return;
        }

        await audioService.ClearAudioCacheAsync();
        await page.DisplayAlertAsync("Hoàn tất", "Đã xóa bộ nhớ audio đã tải.", "Đóng");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var kb = bytes / 1024d;
        if (kb < 1024)
        {
            return $"{kb:F1} KB";
        }

        var mb = kb / 1024d;
        if (mb < 1024)
        {
            return $"{mb:F1} MB";
        }

        var gb = mb / 1024d;
        return $"{gb:F2} GB";
    }


}
using food_market_narrator.Enums;
using food_market_narrator.Views;

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

            case BottomTab.Tour:
                SetActive(TourIcon, TourText);
                break;

            case BottomTab.Favorite:
                SetActive(FavoriteIcon, FavoriteText);
                break;

            case BottomTab.Setting:
                SetActive(SettingIcon, SettingText);
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

        // Set màu cho TourIcon
        if (TourIcon != null)
            TourIcon.TextColor = InactiveColor;
        if (TourText != null)
            TourText.TextColor = InactiveColor;

        // Set màu cho SettingIcon
        if (SettingIcon != null)
            SettingIcon.TextColor = InactiveColor;
        if (SettingText != null)
            SettingText.TextColor = InactiveColor;
    }

    private void SetActive(Label icon, Label text)
    {
        icon.TextColor = ActiveColor;
        text.TextColor = ActiveColor;
    }


    // Mở bản đồ khi nhấn vào MapIcon hoặc MapText
    private async void OpenMap(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MapPage?tourPoiIds=&tourName=");
    }

    // Mở trang OpenMainPage khi nhấn vào HomeIcon hoặc HomeText
    private async void OpenMainPage(object sender, EventArgs e)
    {
        if (Shell.Current?.CurrentPage is MainPage)
        {
            return;
        }

        var navigation = Shell.Current?.Navigation;
        if (navigation?.NavigationStack != null && navigation.NavigationStack.Any(p => p is MainPage))
        {
            while (navigation.NavigationStack.Count > 1 && navigation.NavigationStack[^1] is not MainPage)
            {
                await navigation.PopAsync(false);
            }

            return;
        }

        // Fallback to absolute route when MainPage is not in current stack.
        await Shell.Current.GoToAsync("//MainPage");
    }

    // Mở trang Yêu thích
    private async void OpenFavorite(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//FavoritePage");
    }

    private async void OpenTour(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TourPage");
    }

    private async void OpenSettings(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SettingsPage");
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
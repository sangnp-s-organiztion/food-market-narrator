using food_market_narrator.Enums;
using food_market_narrator.Services;

namespace food_market_narrator.Views.Shared;

public partial class BottomNavigationView : ContentView
{
    private double? _latitude;
    private double? _longtitude;
    private string _locationName;
    private LanguageService? languageService = new LanguageService();

    private LocationServices locationServices = new LocationServices();


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
        // Kiểm tra nếu có tọa độ thì mới truyền tham số để tránh lỗi parse double từ chuỗi rỗng
        if (_latitude.HasValue && _longtitude.HasValue)
        {
            // Sử dụng đường dẫn tuyệt đối (//) cho trang chính (ShellContent)
            await Shell.Current.GoToAsync($"//MapPage?lat={_latitude.Value}&lng={_longtitude.Value}&name={_locationName}");
        }
        else
        {
            // Chỉ chuyển trang nếu không có dữ liệu
            await Shell.Current.GoToAsync("//MapPage");
        }
    }

    // Mở trang OpenMainPage khi nhấn vào HomeIcon hoặc HomeText
    private async void OpenMainPage(object sender, EventArgs e)
    {
        // Use absolute route to reset to the main tab/page
        await Shell.Current.GoToAsync("//MainPage");
    }


}
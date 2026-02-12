using food_market_narrator.Services;
namespace food_market_narrator.Views;
using System.Globalization;
using food_market_narrator.Resources;

public partial class MainPage : ContentPage
{
	private double? lat;
	private double? lng;
	private string name;

	private LocationServices locationServices = new LocationServices();
	private bool _isFirstLoad = true;

	// Hàm khởi tạo MainPage mới
	public MainPage()
	{
		InitializeComponent();
		var mapPage = new MapPage(lat, lng, name);
		// Giả sử bạn đã khởi tạo locationService
		locationServices.OnLocationUpdated += (lat, lng) => 
		{
			MainThread.BeginInvokeOnMainThread(() => 
			{
				LocationLabel.Text = $"lat:{lat:F6}, lng:{lng:F6}";
			});
		};
	}

	// Mở trang bản đồ khi nhấn nút
	private async void OpenMap(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new MapPage(lat, lng, name));
	}

	// Load tất cả những gì cần thiết trước khi hiển thị trang MainPage
    protected override async void OnAppearing()
    {
        base.OnAppearing();
		if (_isFirstLoad)
		{
			_isFirstLoad = false;
			// Nếu chưa từng chọn language thì mới mở popup
            if (!Preferences.ContainsKey("AppLanguage"))
            {
                await OpenLanguagePopup();
            }
		}
		locationServices.StartTrackingLocation();
    }




	// Mở popup (Bạn có thể gọi hàm này khi nhấn vào icon Menu hoặc nút Khóa)
	private async Task OpenLanguagePopup()
	{
		LanguagePopup.IsVisible = true;
		LanguagePopup.Opacity = 0;
		await LanguagePopup.FadeTo(1, 250); // Hiệu ứng hiện dần nhẹ nhàng
	}

	// Đóng popup chọn ngôn ngữ
	private void ClosePopup(object sender, EventArgs e)
	{
		LanguagePopup.IsVisible = false;
	}

	// Khi người dùng chọn ngôn ngữ
	private async void OnLanguageSelected(object sender, EventArgs e)
	{
		var button = (Button)sender;
		string lang = button.CommandParameter.ToString();

		 // Đóng popup mượt
        await LanguagePopup.FadeTo(0, 200);
        LanguagePopup.IsVisible = false;

		ChangeLanguage(lang);
	}


	// Thay đổi ngôn ngữ ứng dụng
	private void ChangeLanguage(string cultureCode)
    {
		// Lưu lại để lần sau app mở tự load
        Preferences.Set("AppLanguage", cultureCode);

        var culture = new CultureInfo(cultureCode);

        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        AppResources.Culture = culture;

        // Reload AppShell (nhẹ hơn recreate toàn app)
        Application.Current.Windows[0].Page = new AppShell();

    }
}
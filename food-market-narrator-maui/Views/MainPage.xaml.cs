using food_market_narrator.Services;
namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
	private double? lat;
	private double? lng;
	private string name;
	private LanguageService? languageService = new LanguageService();

	private LocationServices locationServices = new LocationServices();
	private bool _isFirstLoad = true;

	// Hàm khởi tạo MainPage mới
	public MainPage()
	{
		InitializeComponent();
		
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
        var poiService = App.Current.Handler.MauiContext.Services.GetService<POIService>();
        var narrationService = App.Current.Handler.MauiContext.Services.GetService<NarrationFlowService>();

        var page = new MapPage(
            poiService,
            narrationService,
            lat,
            lng,
            name
        );

        await Navigation.PushAsync(page);
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
		LanguagePopup.FadeTo(0, 250); // Hiệu ứng mờ dần nhẹ nhàng
		LanguagePopup.IsVisible = false;
	}

	 // Khi người dùng chọn ngôn ngữ
	private async void OnLanguageSelected(object sender, EventArgs e)
	{
		var button = (Button)sender;
		string lang = button.CommandParameter.ToString();

		languageService.ChangeLanguage(lang);
	}




}
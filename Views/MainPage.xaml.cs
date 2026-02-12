using System.Threading.Tasks;
using food_market_narrator.Services;
namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
	private double? lat;
	private double? lng;
	private string name;

	private LocationServices locationServices = new LocationServices();

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

	private async void OpenMap(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new MapPage(lat, lng, name));
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		locationServices.StartTrackingLocation();
    }
}

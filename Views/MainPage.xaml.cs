using System.Threading.Tasks;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
	private double? lat;
	private double? lng;
	private string name;

	public MainPage()
	{
		InitializeComponent();
		var mapPage = new MapPage(lat, lng, name);
	}

	private async void OpenMap(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new MapPage(lat, lng, name));
	}
}

using System.Diagnostics;

namespace food_market_narrator.Views;

public partial class POIDetailPage : ContentPage
{
	public POIDetailPage()
	{
		InitializeComponent();
	}

	// Hàm xử lý khi nhấn vào nút back để quay lại trang chính
	private async void OnBackButtonTapped(object sender, EventArgs e)
	{
		Console.WriteLine("Back button tapped, navigating to MainPage");
		await Shell.Current.GoToAsync("//MainPage");
	}
}
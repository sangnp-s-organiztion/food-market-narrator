using food_market_narrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace food_market_narrator.Views;

[QueryProperty(nameof(RestaurantId), "restaurantId")]
public partial class POIDetailPage : ContentPage
{
	private readonly POIService? _poiService;
	private string _restaurantId = string.Empty;

	public string RestaurantId
	{
		get => _restaurantId;
		set
		{
			_restaurantId = Uri.UnescapeDataString(value ?? string.Empty);
			_ = LoadPoiDetailAsync();
		}
	}

	public POIDetailPage()
	{
		InitializeComponent();
		_poiService = Application.Current?.Handler?.MauiContext?.Services.GetService<POIService>();
	}

	private async Task LoadPoiDetailAsync()
	{
		if (_poiService is null || string.IsNullOrWhiteSpace(_restaurantId))
		{
			return;
		}

		var poi = await _poiService.GetPOIByIdAsync(_restaurantId);
		if (poi is null)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			BindingContext = poi;
		});
	}

	// Hàm xử lý khi nhấn vào nút back để quay lại trang chính
	private async void OnBackButtonTapped(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}
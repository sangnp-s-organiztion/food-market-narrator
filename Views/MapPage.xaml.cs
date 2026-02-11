using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using food_market_narrator.Services;


namespace food_market_narrator.Views;

public partial class MapPage : ContentPage
{
	// set giá trị null cho tọa độ (souble?) để tránh lỗi khi không có tọa độ
	private double? _targetLatitude; // vĩ độ
	private double? _targetLongitude; // kinh độ
	private String _targetLocationName; // tên địa điểm
	private bool _isTrackingLocation = false;
	private IDispatcherTimer locationTimer;
	private LocationServices locationServices;

	// Khởi tạo địa điểm của map khi mở map
	public MapPage(double? latitude, double? longitude, String locationName)
	{
		InitializeComponent();
		_targetLatitude = latitude;
		_targetLongitude = longitude;
		_targetLocationName = locationName;

		Loaded += OnMapLoadedAndFocused;
	}

	// Tải bản đồ và focus vào vị trí cần đến khi map được tải xong
	public async void OnMapLoadedAndFocused(object sender, EventArgs e)
	{
		await FocusLocation();
	}

	// Focus vào vị trí cần đến khi map được tải xong
	public async Task FocusLocation()
	{
		try
		{
			Location? location;

			// Kiểm tra lat, lng có tọa độ hay không
			if (_targetLatitude.HasValue && _targetLongitude.HasValue)
			{
				location = new Location(_targetLatitude.Value, _targetLongitude.Value);
			} else
			{
				// Lấy vị trí (lat, lng) hiện tại qua GPS
				// Medium giúp tiết kiệm pin hơn Best, độ chính xác ~10-100m
				var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
				location = await Geolocation.Default.GetLocationAsync(request);
			}


			if (location != null)
			{
				Map.MoveToRegion(
					MapSpan.FromCenterAndRadius(
						location,
						Distance.FromKilometers(1)
					)
				);
			}
		} catch (Exception ex)
		{
			await DisplayAlert("Lỗi", "Không thể tải bản đồ: " + ex.Message, "OK");
		}
	}

    
}
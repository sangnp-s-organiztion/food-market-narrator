using food_market_narrator.Views;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace food_market_narrator.Services;

public class LocationServices
{
    // set giá trị null cho tọa độ (souble?) để tránh lỗi khi không có tọa độ
	private double? _targetLatitude; // vĩ độ
	private double? _targetLongitude; // kinh độ
    private IDispatcherTimer locationTimer;
    public event Action<double, double>? OnLocationUpdated;


    // Lấy vị trí hiện tại của người dùng
	private async Task<(double?, double?)> GetCurrentLocation()
	{
		try
		{
			var request = new GeolocationRequest(GeolocationAccuracy.High);
			var location = await Geolocation.Default.GetLocationAsync(request);

			// Cập nhật lại lat, lng
			if (location != null)
			{
				_targetLatitude = location.Latitude;
				_targetLongitude = location.Longitude;
			}

		} catch (Exception ex)
		{
			Console.WriteLine("Lỗi", "Không thể lấy vị trí hiện tại: " + ex.Message, "OK");
		}

        return (_targetLatitude, _targetLongitude);
	}



	// Tracking vị trí người dùng mỗi 3s
	public void StartTrackingLocation()
    {
        locationTimer = Application.Current.Dispatcher.CreateTimer();
        locationTimer.Interval = TimeSpan.FromSeconds(3);

        locationTimer.Tick += async (s, e) =>
        {
            var (lat, lng) = await GetCurrentLocation();

            if (lat != null && lng != null)
                OnLocationUpdated?.Invoke(lat.Value, lng.Value);
        };

        locationTimer.Start();
    }

    // Load tất cả các POI
    // LoadAllPOIsAsync
}
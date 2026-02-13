using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using food_market_narrator.Services;
using Android.Gms.Maps.Model;



namespace food_market_narrator.Views;

public partial class MapPage : ContentPage
{
	// set giá trị null cho tọa độ (souble?) để tránh lỗi khi không có tọa độ
	private double? _targetLatitude; // vĩ độ
	private double? _targetLongitude; // kinh độ
	private String _targetLocationName; // tên địa điểm
	private bool _isTrackingLocation = false;
	private IDispatcherTimer locationTimer;
	private LocationServices locationServices = new LocationServices();
	private POIService _poiService = new POIService();

	// Khởi tạo địa điểm của map khi mở map
	public MapPage(double? latitude, double? longitude, String locationName)
	{
		InitializeComponent();
		_targetLatitude = latitude;
		_targetLongitude = longitude;
		_targetLocationName = locationName;

		Loaded += OnMapLoadedAndFocused;
	}



	protected override async void OnAppearing()
    {
        base.OnAppearing();
		// Đợi map sẵn sàng TRƯỚC khi thao tác
		CustomMapHandler.OnGoogleMapReady += async (googleMap) =>
		{
			Console.WriteLine("MAP READY EVENT FIRED");
			await LoadAllPOIsAsync();
		};
		StartTrackingLocation();
    }


	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		#if ANDROID
			CustomMapHandler.OnGoogleMapReady -= HandleMapReady;
		#endif

		locationTimer?.Stop();
		_isTrackingLocation = false;
	}

	#if ANDROID
	private async void HandleMapReady(Android.Gms.Maps.GoogleMap map)
	{
		await LoadAllPOIsAsync();
	}
	#endif


	private void StartTrackingLocation()
	{
		if (_isTrackingLocation) return;

		locationTimer = Dispatcher.CreateTimer();
		locationTimer.Interval = TimeSpan.FromSeconds(1);

		locationTimer.Tick += async (s, e) =>
		{
			var request = new GeolocationRequest(
				GeolocationAccuracy.Medium,
				TimeSpan.FromSeconds(3));

			var location = await Geolocation.Default.GetLocationAsync(request);

			if (location == null) return;

			var nearest = _poiService.UpdateNearestPOI(
				location.Latitude,
				location.Longitude);

			if (nearest != null)
			{
				_poiService.HighlightNearestPOI(Map, nearest);
			}
		};

		locationTimer.Start();
		_isTrackingLocation = true;
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

    // Hiển thị tất cả các POIs 
	private async Task LoadAllPOIsAsync()
    {
		
        if (Map == null) return;

        var pois = await _poiService.GetAllPOIsAsync();
        
        // Nếu không có dữ liệu, thoát sớm để tránh lỗi tính toán
        if (pois == null || !pois.Any()) return;

		#if ANDROID
		// Đợi map sẵn sàng TRƯỚC khi thao tác
		

		var googleMap = CustomMapHandler.NativeGoogleMap;
		
		if (googleMap == null)
		{
			Console.WriteLine("Google Map không sẵn sàng sau khi đợi");
			return;
		}		
		// Clear tất cả markers cũ
		googleMap.Clear();
		CustomMapHandler.MarkerDictionary.Clear();
		#endif

        foreach (var poi in pois)
        {
			#if ANDROID
			var marker = googleMap.AddMarker(new MarkerOptions()
				.SetPosition(new LatLng(poi.Latitude, poi.Longitude))
				.SetTitle(poi.Name)
				.SetSnippet(poi.Description)
				.SetIcon(BitmapDescriptorFactory.DefaultMarker(
					BitmapDescriptorFactory.HueRed)));

			CustomMapHandler.MarkerDictionary[poi.Id] = marker;
			#endif
			// iOS thì giữ MAUI pin ở đây
			// var pin = new Pin
			// {
			// 	Label = poi.Name,
			// 	Address = poi.Description,
			// 	Type = PinType.Place,
			// 	Location = new Location(poi.Latitude, poi.Longitude)
			// };

			// poi.MapPin = pin;
			// Map.Pins.Add(pin);
        }


        // Viết hàm tính Bounding Box thay cho FromLocations ---
        // 1. Tìm cực điểm vĩ độ và kinh độ
        double minLat = pois.Min(p => p.Latitude);
        double maxLat = pois.Max(p => p.Latitude);
        double minLon = pois.Min(p => p.Longitude);
        double maxLon = pois.Max(p => p.Longitude);

        // 2. Tính điểm trung tâm
        double centerLat = (minLat + maxLat) / 2;
        double centerLon = (minLon + maxLon) / 2;

        // 3. Tính độ chênh lệch (Delta) 
        // Nhân thêm 1.2 (thêm 20% lề) để các Pin không bị dính sát mép màn hình
        double latDelta = (maxLat - minLat) * 1.2;
        double lonDelta = (maxLon - minLon) * 1.2;

        // 4. Di chuyển bản đồ (giới hạn độ Delta tối thiểu để không bị zoom quá sâu nếu chỉ có 1 điểm)
        Map.MoveToRegion(new MapSpan(
            new Location(centerLat, centerLon), 
            Math.Max(latDelta, 0.01), 
            Math.Max(lonDelta, 0.01)
        ));
    }




}
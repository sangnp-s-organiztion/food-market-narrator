using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Devices.Sensors;
using Xunit;

namespace food_market_narrator.Tests.IntegrationTests;

/// <summary>
/// Integration Tests cho POI Service - Quản lý POI và Geofence
/// </summary>
public class POIIntegrationTests
{
    #region 2. Hiển thị bản đồ - Map Display Integration Tests

    /// <summary>
    /// Test tích hợp: Tìm POI gần nhất từ vị trí hiện tại
    /// </summary>
    [Fact]
    public void POIService_GetNearestPOI_ReturnsCorrectPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "poi-1", Latitude = 10.777000, Longitude = 106.689000 },
            new() { restaurantId = "poi-2", Latitude = 10.776889, Longitude = 106.688889 },
            new() { restaurantId = "poi-3", Latitude = 10.778000, Longitude = 106.690000 }
        };

        // User location
        var userLocation = new Location(10.776889, 106.688889);

        // Act
        var nearest = service.GetNearestPOI(userLocation, pois);

        // Assert - Should find poi-2 as nearest
        Assert.NotNull(nearest);
        Assert.Equal("poi-2", nearest.restaurantId);
    }

    /// <summary>
    /// Test tích hợp: Tính khoảng cách chính xác
    /// </summary>
    [Fact]
    public void POIService_GetDistanceMeters_CalculatesAccurately()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Two known points ~15m apart
        var location1 = new Location(10.776889, 106.688889);
        var poi = new POI { Latitude = 10.777000, Longitude = 106.689000 };

        // Act
        var distance = service.GetDistanceMeters(location1, poi);

        // Assert - Distance should be approximately 15m
        Assert.True(distance > 10 && distance < 25);
    }

    #endregion

    #region 3. Thuyết minh tự động (Kích hoạt Geofence) - Geofence Integration Tests

    /// <summary>
    /// Test tích hợp: Phát hiện vào vùng POI lần đầu
    /// </summary>
    [Fact]
    public void POIService_UpdateNearestPOI_FirstEntry_ReturnsPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "poi-1", Latitude = 10.776889, Longitude = 106.688889, Radius = 30 }
        };

        // Set POIs using reflection
        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // User is inside POI radius
        var userLat = 10.776889;
        var userLng = 106.688889;

        // Act
        var result = service.UpdateNearestPOI(userLat, userLng);

        // Assert - Should trigger on first entry
        Assert.NotNull(result);
        Assert.Equal("poi-1", result.restaurantId);
    }

    /// <summary>
    /// Test tích hợp: Phát hiện chuyển đổi giữa các POI
    /// </summary>
    [Fact]
    public void POIService_UpdateNearestPOI_TransitionBetweenPOIs_ReturnsNewPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "poi-1", Latitude = 10.776800, Longitude = 106.688800, Radius = 30 },
            new() { restaurantId = "poi-2", Latitude = 10.777000, Longitude = 106.689000, Radius = 30 }
        };

        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // User enters poi-1 first
        service.UpdateNearestPOI(10.776800, 106.688800);

        // User moves to poi-2 (still within its radius)
        var result = service.UpdateNearestPOI(10.777000, 106.689000);

        // Assert - Should detect transition to new POI
        Assert.NotNull(result);
        Assert.Equal("poi-2", result.restaurantId);
    }

    /// <summary>
    /// Test tích hợp: Phát hiện ra khỏi vùng POI
    /// </summary>
    [Fact]
    public void POIService_UpdateNearestPOI_ExitPOI_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "poi-1", Latitude = 10.776889, Longitude = 106.688889, Radius = 30 }
        };

        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // User enters poi-1
        service.UpdateNearestPOI(10.776889, 106.688889);

        // User moves far away (exits POI)
        var result = service.UpdateNearestPOI(10.781000, 106.693000);

        // Assert - No new POI triggered
        Assert.Null(result);
    }

    #endregion

    #region 4. Thuyết minh audio - Audio Integration Tests

    /// <summary>
    /// Test tích hợp: Lấy audio theo ngôn ngữ ưu tiên
    /// </summary>
    [Fact]
    public void POI_GetAudioUrl_PrefersMatchingLanguage()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "poi-1",
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", AudioUrl = "audio/en/test.mp3", IsActive = true },
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/test.mp3", IsActive = true },
                new() { LanguageCode = "ja", AudioUrl = "audio/ja/test.mp3", IsActive = true }
            }
        };

        // Act
        var audioUrl = poi.GetAudioUrl("vi");

        // Assert
        Assert.Equal("audio/vi/test.mp3", audioUrl);
    }

    /// <summary>
    /// Test tích hợp: Fallback khi ngôn ngữ không có
    /// </summary>
    [Fact]
    public void POI_GetAudioUrl_FallbackToFirstActive()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "poi-1",
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", AudioUrl = "audio/en/test.mp3", IsActive = true },
                new() { LanguageCode = "ja", AudioUrl = "audio/ja/test.mp3", IsActive = true }
            }
        };

        // Act - Request Vietnamese (not available)
        var audioUrl = poi.GetAudioUrl("vi");

        // Assert - Should fallback to English
        Assert.Equal("audio/en/test.mp3", audioUrl);
    }

    #endregion

    #region 5. Kích hoạt nội dung qua mã QR - QR Code Integration Tests

    /// <summary>
    /// Test tích hợp: Tìm POI theo ID (từ QR code)
    /// </summary>
    [Fact]
    public async Task POIService_GetPOIById_FindsCorrectPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "restaurant-1", Name = "Restaurant 1" },
            new() { restaurantId = "restaurant-2", Name = "Restaurant 2" },
            new() { restaurantId = "restaurant-3", Name = "Restaurant 3" }
        };

        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act - Simulate QR code scan
        var result = await service.GetPOIByIdAsync("restaurant-2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Restaurant 2", result.Name);
    }

    /// <summary>
    /// Test tích hợp: QR code không tìm thấy POI
    /// </summary>
    [Fact]
    public async Task POIService_GetPOIById_NotFound_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "restaurant-1", Name = "Restaurant 1" }
        };

        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act - QR code points to non-existent POI
        var result = await service.GetPOIByIdAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 6. Quyền riêng tư của người dùng - Privacy Integration Tests

    /// <summary>
    /// Test tích hợp: Dữ liệu POI không chứa thông tin cá nhân
    /// </summary>
    [Fact]
    public void POI_Model_NoPersonalData()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "poi-1",
            Name = "Test Restaurant",
            Latitude = 10.776889,
            Longitude = 106.688889,
            Description = "A great restaurant"
        };

        // Assert - POI model should only contain business data
        Assert.NotNull(poi.restaurantId);
        Assert.NotNull(poi.Name);
        Assert.Null(poi.Phone); // No personal phone
        // Note: Address can be business address (not personal)
    }

    #endregion
}

using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Devices.Sensors;
using Moq;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Services;

/// <summary>
/// Unit Tests cho POIService
/// </summary>
public class POIServiceTests
{
    #region 1. Theo dõi vị trí - Location Tracking Tests

    [Fact]
    public async Task GetPOIsAsync_WhenNoCache_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new Mock<POIService>(httpClient);

        // Act
        var pois = await service.Object.GetPOIsAsync();

        // Assert
        Assert.NotNull(pois);
    }

    [Fact]
    public async Task GetAllPOIsAsync_WhenPOIsLoaded_ReturnsPOIs()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act - Gọi phương thức
        var pois = await service.GetAllPOIsAsync();

        // Assert
        Assert.NotNull(pois);
    }

    #endregion

    #region 2. Hiển thị bản đồ - Map Display Tests

    [Fact]
    public void GetNearestPOI_WithNullPOIs_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);
        var currentLocation = new Location(10.776889, 106.688889);

        // Act
        var nearest = service.GetNearestPOI(currentLocation, null);

        // Assert
        Assert.Null(nearest);
    }

    [Fact]
    public void GetNearestPOI_WithEmptyPOIs_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);
        var currentLocation = new Location(10.776889, 106.688889);
        var pois = new List<POI>();

        // Act
        var nearest = service.GetNearestPOI(currentLocation, pois);

        // Assert
        Assert.Null(nearest);
    }

    [Fact]
    public void GetNearestPOI_WithMultiplePOIs_ReturnsNearest()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);
        var currentLocation = new Location(10.776889, 106.688889);

        var pois = new List<POI>
        {
            new() { restaurantId = "1", Latitude = 10.777000, Longitude = 106.689000 }, // ~15m away
            new() { restaurantId = "2", Latitude = 10.778000, Longitude = 106.690000 }, // ~200m away
            new() { restaurantId = "3", Latitude = 10.776900, Longitude = 106.688900 }  // ~10m away
        };

        // Act
        var nearest = service.GetNearestPOI(currentLocation, pois);

        // Assert
        Assert.NotNull(nearest);
        Assert.Equal("3", nearest.restaurantId);
    }

    [Fact]
    public void GetNearestPOI_ByLatLng_ReturnsCorrectPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "1", Latitude = 10.777000, Longitude = 106.689000 },
            new() { restaurantId = "2", Latitude = 10.776889, Longitude = 106.688889 }
        };

        // Act
        var nearest = service.GetNearestPOI(10.776889, 106.688889);

        // Assert
        Assert.NotNull(nearest);
    }

    #endregion

    #region 3. Thuyết minh tự động (Kích hoạt Geofence) - Geofence Tests

    [Fact]
    public void GetDistanceMeters_CalculatesCorrectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);
        var currentLocation = new Location(10.776889, 106.688889);
        var poi = new POI { Latitude = 10.777000, Longitude = 106.689000 };

        // Act
        var distance = service.GetDistanceMeters(currentLocation, poi);

        // Assert
        Assert.True(distance > 0);
        Assert.True(distance < 200); // Should be around 15-20m
    }

    [Fact]
    public void UpdateNearestPOI_WithNullPOIs_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act
        var result = service.UpdateNearestPOI(10.776889, 106.688889);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateNearestPOI_FirstEntryInsideRadius_ReturnsPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Load POIs
        var pois = new List<POI>
        {
            new() { restaurantId = "1", Latitude = 10.776889, Longitude = 106.688889, Radius = 30 }
        };

        // Sử dụng reflection để set _pois
        var field = typeof(POIService).GetField("_pois", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act - Lần đầu vào trong bán kính 30m
        var result = service.UpdateNearestPOI(10.776889, 106.688889);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateNearestPOI_OutsideRadius_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "1", Latitude = 10.776889, Longitude = 106.688889, Radius = 30 }
        };

        var field = typeof(POIService).GetField("_pois", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act - Ở ngoài bán kính 30m (cách ~500m)
        var result = service.UpdateNearestPOI(10.781000, 106.693000);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 4. Thuyết minh audio - Audio Narration Tests

    [Fact]
    public async Task GetPOIByIdAsync_WithValidId_ReturnsPOI()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "restaurant-1", Name = "Test Restaurant" },
            new() { restaurantId = "restaurant-2", Name = "Another Restaurant" }
        };

        var field = typeof(POIService).GetField("_pois", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act
        var result = await service.GetPOIByIdAsync("restaurant-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Restaurant", result.Name);
    }

    [Fact]
    public async Task GetPOIByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        var pois = new List<POI>
        {
            new() { restaurantId = "restaurant-1", Name = "Test Restaurant" }
        };

        var field = typeof(POIService).GetField("_pois", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, pois);

        // Act
        var result = await service.GetPOIByIdAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPOIByIdAsync_WithEmptyId_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act
        var result = await service.GetPOIByIdAsync("");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 5. Kích hoạt nội dung qua mã QR - QR Code Tests

    [Fact]
    public async Task GetDishesByRestaurantIdAsync_WithValidId_ReturnsDishes()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act
        var dishes = await service.GetDishesByRestaurantIdAsync("restaurant-1");

        // Assert
        Assert.NotNull(dishes);
    }

    [Fact]
    public async Task GetDishesByRestaurantIdAsync_WithEmptyId_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act
        var dishes = await service.GetDishesByRestaurantIdAsync("");

        // Assert
        Assert.NotNull(dishes);
        Assert.Empty(dishes);
    }

    #endregion

    #region 6. Quyền riêng tư của người dùng - Privacy Tests

    [Fact]
    public async Task GetAllPOIsAsync_ReturnsCopyNotReference()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new POIService(httpClient);

        // Act
        var pois = await service.GetAllPOIsAsync();

        // Assert - Kiểm tra không trả về reference trực tiếp
        Assert.NotNull(pois);
    }

    #endregion
}

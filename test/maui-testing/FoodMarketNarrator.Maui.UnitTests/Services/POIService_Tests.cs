using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Devices.Sensors;

namespace unit_test.Services;

/// <summary>
/// Unit tests for POIService logic (distance calculation, nearest POI, geofence)
/// Note: Network/cache logic not tested here
/// </summary>
public class POIService_Tests
{
    #region GetDistanceMeters Tests

    [Fact]
    public void GetDistanceMeters_SameLocation_ReturnsZero()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        var location = new Location(10.776889, 106.6890608);

        // Act
        var result = locationService.GetDistanceMeters(location, poi);

        // Assert
        Assert.Equal(0, result, 1);
    }

    [Fact]
    public void GetDistanceMeters_DifferentLocations_ReturnsDistance()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        // Location about 30 meters away
        var location = new Location(10.777169, 106.6890608);

        // Act
        var result = locationService.GetDistanceMeters(location, poi);

        // Assert
        Assert.True(result > 25 && result < 35, $"Expected ~30m, got {result}m");
    }

    #endregion

    #region GetNearestPOI Tests

    [Fact]
    public void GetNearestPOI_EmptyList_ReturnsNull()
    {
        // Arrange
        var locationService = CreatePOIServiceWithPois(new List<POI>());
        var location = new Location(10.776889, 106.6890608);

        // Act
        var result = locationService.GetNearestPOI(location);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNearestPOI_NullList_ReturnsNull()
    {
        // Arrange
        var locationService = CreatePOIServiceWithPois(null!);
        var location = new Location(10.776889, 106.6890608);

        // Act
        var result = locationService.GetNearestPOI(location);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNearestPOI_SinglePOI_ReturnsThatPOI()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Name = "Restaurant 1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });
        var location = new Location(10.777169, 106.6890608);

        // Act
        var result = locationService.GetNearestPOI(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("resto1", result.restaurantId);
    }

    [Fact]
    public void GetNearestPOI_MultiplePOIs_ReturnsNearest()
    {
        // Arrange
        var poi1 = new POI
        {
            restaurantId = "resto1",
            Name = "Restaurant 1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var poi2 = new POI
        {
            restaurantId = "resto2",
            Name = "Restaurant 2",
            Latitude = 10.777889,
            Longitude = 106.6900608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi1, poi2 });

        // Location closer to poi1
        var location = new Location(10.776889, 106.6890608);

        // Act
        var result = locationService.GetNearestPOI(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("resto1", result.restaurantId);
    }

    [Fact]
    public void GetNearestPOI_WithCoordinates_ReturnsNearest()
    {
        // Arrange
        var poi1 = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var poi2 = new POI
        {
            restaurantId = "resto2",
            Latitude = 10.777889,
            Longitude = 106.6900608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi1, poi2 });

        // Act
        var result = locationService.GetNearestPOI(10.776889, 106.6890608);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("resto1", result.restaurantId);
    }

    #endregion

    #region UpdateNearestPOI Tests - Geofence Logic

    [Fact]
    public void UpdateNearestPOI_FirstEnter_ReturnsPOI()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        // User is within 30m (enter radius)
        // Act
        var result = locationService.UpdateNearestPOI(10.776889, 106.6890608);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("resto1", result.restaurantId);
    }

    [Fact]
    public void UpdateNearestPOI_OutsideRadius_ReturnsNull()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        // User is far outside (more than 30m)
        // Act
        var result = locationService.UpdateNearestPOI(10.779889, 106.6920608);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateNearestPOI_EnterThenStay_ReturnsNull()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        // First enter
        var firstResult = locationService.UpdateNearestPOI(10.776889, 106.6890608);
        Assert.NotNull(firstResult);

        // Stay at same location (should return null - no new transition)
        var secondResult = locationService.UpdateNearestPOI(10.776889, 106.6890608);

        // Assert
        Assert.Null(secondResult);
    }

    [Fact]
    public void UpdateNearestPOI_EnterThenExit_ReturnsNull()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };

        var locationService = CreatePOIServiceWithPois(new List<POI> { poi });

        // First enter (within 30m)
        locationService.UpdateNearestPOI(10.776889, 106.6890608);

        // Then exit (more than 40m - exit radius)
        var exitResult = locationService.UpdateNearestPOI(10.780889, 106.6930608);

        // Assert
        Assert.Null(exitResult);
    }

    [Fact]
    public void UpdateNearestPOI_EmptyPOIs_ReturnsNull()
    {
        // Arrange
        var locationService = CreatePOIServiceWithPois(new List<POI>());

        // Act
        var result = locationService.UpdateNearestPOI(10.776889, 106.6890608);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateNearestPOI_NullPOIs_ReturnsNull()
    {
        // Arrange
        var locationService = CreatePOIServiceWithPois(null!);

        // Act
        var result = locationService.UpdateNearestPOI(10.776889, 106.6890608);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a POIService instance and sets the internal _pois field using reflection
    /// </summary>
    private POIService CreatePOIServiceWithPois(List<POI> pois)
    {
        // Create a mock HttpClient (not actually used in these tests)
        var httpClient = new HttpClient();

        // Create the service
        var service = new POIService(httpClient);

        // Use reflection to set the private _pois field
        var field = typeof(POIService).GetField("_pois",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(service, pois);

        return service;
    }

    #endregion
}

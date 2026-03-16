using food_market_narrator.Settings;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Settings;

/// <summary>
/// Unit Tests cho AppSettings - Cấu hình ứng dụng
/// </summary>
public class AppSettingsTests
{
    #region API Settings Tests

    [Fact]
    public void AppSettings_HasApiBaseUrl()
    {
        // Act
        var apiBaseUrl = AppSettings.ApiBaseUrl;

        // Assert
        Assert.NotNull(apiBaseUrl);
        Assert.Contains("http://", apiBaseUrl);
    }

    [Fact]
    public void AppSettings_HasApiFallbackBaseUrls()
    {
        // Act
        var fallbackUrls = AppSettings.ApiFallbackBaseUrls;

        // Assert
        Assert.NotNull(fallbackUrls);
        Assert.NotEmpty(fallbackUrls);
    }

    #endregion

    #region Endpoint Tests

    [Fact]
    public void AppSettings_HasRestaurantEndpoint()
    {
        // Act
        var endpoint = AppSettings.RestaurantEndpoint;

        // Assert
        Assert.Equal("restaurant", endpoint);
    }

    [Fact]
    public void AppSettings_HasLanguageEndpoint()
    {
        // Act
        var endpoint = AppSettings.LanguageEndpoint;

        // Assert
        Assert.Equal("language", endpoint);
    }

    #endregion

    #region Map Settings Tests

    [Fact]
    public void AppSettings_MapHighlightDistance_IsSet()
    {
        // Act
        var distance = AppSettings.MapHighlightDistanceMeters;

        // Assert
        Assert.True(distance > 0);
        Assert.Equal(20, distance);
    }

    #endregion

    #region Geofence Settings Tests

    [Fact]
    public void AppSettings_TriggerDistanceMeters_IsSet()
    {
        // Act
        var distance = AppSettings.TriggerDistanceMeters;

        // Assert
        Assert.True(distance > 0);
        Assert.Equal(30, distance);
    }

    [Fact]
    public void AppSettings_PoiEnterRadiusMeters_IsSet()
    {
        // Act
        var radius = AppSettings.PoiEnterRadiusMeters;

        // Assert
        Assert.True(radius > 0);
        Assert.Equal(30, radius);
    }

    [Fact]
    public void AppSettings_PoiExitRadiusMeters_IsSet()
    {
        // Act
        var radius = AppSettings.PoiExitRadiusMeters;

        // Assert
        Assert.True(radius > 0);
        Assert.Equal(40, radius);
    }

    [Fact]
    public void AppSettings_ExitRadius_GreaterThanEnterRadius()
    {
        // Assert - Exit radius should be greater than enter radius for proper hysteresis
        Assert.True(AppSettings.PoiExitRadiusMeters > AppSettings.PoiEnterRadiusMeters);
    }

    #endregion
}

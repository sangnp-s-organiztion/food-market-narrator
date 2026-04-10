using food_market_narrator.Services;

namespace unit_test.Services;

public class QrAccessService_Tests
{
    [Theory]
    [InlineData("foodmarketnarrator://open")]
    [InlineData("foodmarketnarrator://open?durationMinutes=30")]
    [InlineData("foodmarketnarrator://open?foo=bar")]
    public void ApplyDeepLink_WithValidSchemeAndHost_DoesNotThrow(string deepLink)
    {
        // Arrange
        var service = new QrAccessService();

        // Act
        var exception = Record.Exception(() => service.ApplyDeepLink(deepLink));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("foodmarketnarrator://invalid")]
    [InlineData("not-a-uri")]
    [InlineData("")]
    public void ApplyDeepLink_WithInvalidLink_DoesNotThrow(string deepLink)
    {
        // Arrange
        var service = new QrAccessService();

        // Act
        var exception = Record.Exception(() => service.ApplyDeepLink(deepLink));

        // Assert
        Assert.Null(exception);
    }
}

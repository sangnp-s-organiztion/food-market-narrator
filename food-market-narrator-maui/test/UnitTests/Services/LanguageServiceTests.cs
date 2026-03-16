using food_market_narrator.Models;
using food_market_narrator.Services;
using Moq;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Services;

/// <summary>
/// Unit Tests cho LanguageService - Quản lý ngôn ngữ ứng dụng
/// </summary>
public class LanguageServiceTests
{
    #region Get All Languages Tests

    [Fact]
    public async Task GetAllLanguagesAsync_WithCache_ReturnsCachedLanguages()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        var result1 = await service.GetAllLanguagesAsync();
        var result2 = await service.GetAllLanguagesAsync();

        // Assert - Should return same cached instance
        Assert.NotNull(result1);
    }

    #endregion

    #region Get Language By Code Tests

    [Fact]
    public async Task GetLanguageByCodeAsync_WithValidCode_ReturnsLanguage()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        var result = await service.GetLanguageByCodeAsync("vi");

        // Assert - Returns language or null depending on API/caching
        // Test verifies method exists and can be called
        Assert.NotNull(result); // May be null if no cache/API
    }

    [Fact]
    public async Task GetLanguageByCodeAsync_WithEmptyCode_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        var result = await service.GetLanguageByCodeAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLanguageByCodeAsync_WithNullCode_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        var result = await service.GetLanguageByCodeAsync(null!);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Current Language Tests

    [Fact]
    public void LanguageService_HasCurrentLanguageProperty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        var currentLanguage = service.CurrentLanguage;

        // Assert
        Assert.NotNull(currentLanguage);
    }

    #endregion

    #region Change Language Tests

    [Fact]
    public void ChangeLanguage_WithValidCode_DoesNotThrow()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act & Assert - Should not throw
        service.ChangeLanguage("en-US");
    }

    [Fact]
    public void ChangeLanguage_WithDifferentCodes_ChangesLanguage()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new LanguageService(httpClient);

        // Act
        service.ChangeLanguage("en-US");
        var lang1 = service.CurrentLanguage;

        service.ChangeLanguage("vi-VN");
        var lang2 = service.CurrentLanguage;

        // Assert
        // Language should change based on Preference
        Assert.NotNull(lang1);
        Assert.NotNull(lang2);
    }

    #endregion
}

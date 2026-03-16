using food_market_narrator.Services;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Services;

/// <summary>
/// Unit Tests cho FavoriteService - Quản lý danh sách yêu thích
/// </summary>
public class FavoriteServiceTests
{
    private readonly FavoriteService _service;

    public FavoriteServiceTests()
    {
        _service = new FavoriteService();
    }

    #region Get Favorites Tests

    [Fact]
    public void GetFavorites_EmptyFavorites_ReturnsEmptyList()
    {
        // Act
        var result = _service.GetFavorites();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GetFavorites_ReturnsCopyNotReference()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");

        // Act
        var favorites1 = _service.GetFavorites();
        var favorites2 = _service.GetFavorites();

        // Assert - Modifying one list should not affect the other
        favorites1.Add("test");
        Assert.DoesNotContain("test", favorites2);
    }

    #endregion

    #region Add Favorite Tests

    [Fact]
    public void AddFavorite_ValidId_AddsToFavorites()
    {
        // Arrange
        var restaurantId = "restaurant-1";

        // Act
        _service.AddFavorite(restaurantId);
        var result = _service.GetFavorites();

        // Assert
        Assert.Single(result);
        Assert.Equal(restaurantId, result[0]);
    }

    [Fact]
    public void AddFavorite_DuplicateId_DoesNotAddDuplicate()
    {
        // Arrange
        var restaurantId = "restaurant-1";
        _service.AddFavorite(restaurantId);

        // Act
        _service.AddFavorite(restaurantId);
        var result = _service.GetFavorites();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void AddFavorite_EmptyId_DoesNotAdd()
    {
        // Arrange
        var restaurantId = "";

        // Act
        _service.AddFavorite(restaurantId);
        var result = _service.GetFavorites();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AddFavorite_NullId_DoesNotAdd()
    {
        // Arrange
        string? restaurantId = null;

        // Act
        _service.AddFavorite(restaurantId!);
        var result = _service.GetFavorites();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AddFavorite_WhitespaceId_DoesNotAdd()
    {
        // Arrange
        var restaurantId = "   ";

        // Act
        _service.AddFavorite(restaurantId);
        var result = _service.GetFavorites();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Remove Favorite Tests

    [Fact]
    public void RemoveFavorite_ExistingId_RemovesFromFavorites()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");
        _service.AddFavorite("restaurant-2");

        // Act
        _service.RemoveFavorite("restaurant-1");
        var result = _service.GetFavorites();

        // Assert
        Assert.Single(result);
        Assert.Equal("restaurant-2", result[0]);
    }

    [Fact]
    public void RemoveFavorite_NonExistingId_DoesNotThrow()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");

        // Act & Assert - Should not throw
        _service.RemoveFavorite("non-existing");
    }

    #endregion

    #region Is Favorite Tests

    [Fact]
    public void IsFavorite_ExistingId_ReturnsTrue()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");

        // Act
        var result = _service.IsFavorite("restaurant-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFavorite_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");

        // Act
        var result = _service.IsFavorite("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFavorite_AfterRemove_ReturnsFalse()
    {
        // Arrange
        _service.AddFavorite("restaurant-1");
        _service.RemoveFavorite("restaurant-1");

        // Act
        var result = _service.IsFavorite("restaurant-1");

        // Assert
        Assert.False(result);
    }

    #endregion
}

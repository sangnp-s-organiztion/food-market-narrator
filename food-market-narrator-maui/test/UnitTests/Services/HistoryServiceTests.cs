using food_market_narrator.Services;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Services;

/// <summary>
/// Unit Tests cho HistoryService - Quản lý lịch sử xem quán
/// </summary>
public class HistoryServiceTests
{
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _service = new HistoryService();
    }

    #region History Management Tests

    [Fact]
    public void GetHistory_EmptyHistory_ReturnsEmptyList()
    {
        // Act
        var result = _service.GetHistory();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_ValidId_AddsToHistory()
    {
        // Arrange
        var restaurantId = "restaurant-1";

        // Act
        _service.AddToHistory(restaurantId);
        var result = _service.GetHistory();

        // Assert
        Assert.Single(result);
        Assert.Equal(restaurantId, result[0]);
    }

    [Fact]
    public void AddToHistory_EmptyId_DoesNotAdd()
    {
        // Arrange
        var restaurantId = "";

        // Act
        _service.AddToHistory(restaurantId);
        var result = _service.GetHistory();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_NullId_DoesNotAdd()
    {
        // Arrange
        string? restaurantId = null;

        // Act
        _service.AddToHistory(restaurantId!);
        var result = _service.GetHistory();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_WhitespaceId_DoesNotAdd()
    {
        // Arrange
        var restaurantId = "   ";

        // Act
        _service.AddToHistory(restaurantId);
        var result = _service.GetHistory();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_DuplicateId_MovesToTop()
    {
        // Arrange
        var restaurantId = "restaurant-1";
        _service.AddToHistory(restaurantId);
        _service.AddToHistory("restaurant-2");

        // Act
        _service.AddToHistory(restaurantId);
        var result = _service.GetHistory();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(restaurantId, result[0]); // Moved to top
    }

    [Fact]
    public void AddToHistory_MultipleIds_OrdersCorrectly()
    {
        // Act
        _service.AddToHistory("restaurant-1");
        _service.AddToHistory("restaurant-2");
        _service.AddToHistory("restaurant-3");
        var result = _service.GetHistory();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("restaurant-3", result[0]); // Most recent first
        Assert.Equal("restaurant-2", result[1]);
        Assert.Equal("restaurant-1", result[2]);
    }

    [Fact]
    public void AddToHistory_ExceedsMaxItems_RemovesOldest()
    {
        // Arrange - Add more than MaxHistoryItems (50)
        for (int i = 0; i < 55; i++)
        {
            _service.AddToHistory($"restaurant-{i}");
        }

        // Act
        var result = _service.GetHistory();

        // Assert - Should only keep 50
        Assert.Equal(50, result.Count);
        Assert.Equal("restaurant-54", result[0]); // Most recent
        Assert.Equal("restaurant-4", result[49]); // Oldest kept
    }

    #endregion

    #region Remove from History Tests

    [Fact]
    public void RemoveFromHistory_ExistingId_RemovesFromHistory()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");
        _service.AddToHistory("restaurant-2");

        // Act
        _service.RemoveFromHistory("restaurant-1");
        var result = _service.GetHistory();

        // Assert
        Assert.Single(result);
        Assert.Equal("restaurant-2", result[0]);
    }

    [Fact]
    public void RemoveFromHistory_NonExistingId_DoesNotThrow()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");

        // Act & Assert - Should not throw
        _service.RemoveFromHistory("non-existing");
    }

    #endregion

    #region Clear History Tests

    [Fact]
    public void ClearHistory_WithItems_RemovesAll()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");
        _service.AddToHistory("restaurant-2");

        // Act
        _service.ClearHistory();
        var result = _service.GetHistory();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ClearHistory_EmptyHistory_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.ClearHistory();
    }

    #endregion

    #region Is In History Tests

    [Fact]
    public void IsInHistory_ExistingId_ReturnsTrue()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");

        // Act
        var result = _service.IsInHistory("restaurant-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInHistory_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");

        // Act
        var result = _service.IsInHistory("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInHistory_AfterRemove_ReturnsFalse()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");
        _service.RemoveFromHistory("restaurant-1");

        // Act
        var result = _service.IsInHistory("restaurant-1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInHistory_AfterClear_ReturnsFalse()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");
        _service.ClearHistory();

        // Act
        var result = _service.IsInHistory("restaurant-1");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 6. Quyền riêng tư của người dùng - Privacy Tests

    [Fact]
    public void GetHistory_ReturnsCopyNotReference()
    {
        // Arrange
        _service.AddToHistory("restaurant-1");

        // Act
        var history1 = _service.GetHistory();
        var history2 = _service.GetHistory();

        // Assert - Modifying one list should not affect the other
        history1.Add("test");
        Assert.DoesNotContain("test", history2);
    }

    #endregion
}

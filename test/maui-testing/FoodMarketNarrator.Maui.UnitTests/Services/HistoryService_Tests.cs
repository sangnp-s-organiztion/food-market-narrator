using food_market_narrator.Services;

namespace unit_test.Services;

/// <summary>
/// Unit tests for HistoryService
/// </summary>
public class HistoryService_Tests
{
    private readonly HistoryService _historyService;

    public HistoryService_Tests()
    {
        _historyService = new HistoryService();
    }

    #region AddToHistory Tests

    [Fact]
    public void AddToHistory_NewItem_AddsToBeginning()
    {
        // Act
        _historyService.AddToHistory("resto1");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Single(result);
        Assert.Equal("resto1", result[0]);
    }

    [Fact]
    public void AddToHistory_MultipleItems_AddsInOrder()
    {
        // Act
        _historyService.AddToHistory("resto1");
        _historyService.AddToHistory("resto2");
        _historyService.AddToHistory("resto3");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Equal(3, result.Count);
        Assert.Equal("resto3", result[0]);
        Assert.Equal("resto2", result[1]);
        Assert.Equal("resto1", result[2]);
    }

    [Fact]
    public void AddToHistory_ExistingItem_MovesToBeginning()
    {
        // Arrange
        _historyService.AddToHistory("resto1");
        _historyService.AddToHistory("resto2");

        // Act
        _historyService.AddToHistory("resto1");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Equal("resto1", result[0]);
        Assert.Equal("resto2", result[1]);
    }

    [Fact]
    public void AddToHistory_EmptyString_DoesNothing()
    {
        // Act
        _historyService.AddToHistory("");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_NullString_DoesNothing()
    {
        // Act
        _historyService.AddToHistory(null!);

        // Assert
        var result = _historyService.GetHistory();
        Assert.Empty(result);
    }

    [Fact]
    public void AddToHistory_WhitespaceString_DoesNothing()
    {
        // Act
        _historyService.AddToHistory("   ");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Empty(result);
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public void GetHistory_Empty_ReturnsEmptyList()
    {
        // Act
        var result = _historyService.GetHistory();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetHistory_ReturnsCopy_NotReference()
    {
        // Arrange
        _historyService.AddToHistory("resto1");

        // Act
        var result = _historyService.GetHistory();
        result.Add("another");

        // Assert
        var original = _historyService.GetHistory();
        Assert.Single(original);
    }

    #endregion

    #region RemoveFromHistory Tests

    [Fact]
    public void RemoveFromHistory_ExistingItem_RemovesItem()
    {
        // Arrange
        _historyService.AddToHistory("resto1");
        _historyService.AddToHistory("resto2");

        // Act
        _historyService.RemoveFromHistory("resto1");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Single(result);
        Assert.Equal("resto2", result[0]);
    }

    [Fact]
    public void RemoveFromHistory_NonExistingItem_DoesNothing()
    {
        // Arrange
        _historyService.AddToHistory("resto1");

        // Act
        _historyService.RemoveFromHistory("resto2");

        // Assert
        var result = _historyService.GetHistory();
        Assert.Single(result);
    }

    #endregion

    #region ClearHistory Tests

    [Fact]
    public void ClearHistory_WithItems_ClearsAll()
    {
        // Arrange
        _historyService.AddToHistory("resto1");
        _historyService.AddToHistory("resto2");

        // Act
        _historyService.ClearHistory();

        // Assert
        var result = _historyService.GetHistory();
        Assert.Empty(result);
    }

    [Fact]
    public void ClearHistory_Empty_DoesNothing()
    {
        // Act
        _historyService.ClearHistory();

        // Assert
        var result = _historyService.GetHistory();
        Assert.Empty(result);
    }

    #endregion

    #region IsInHistory Tests

    [Fact]
    public void IsInHistory_ExistingItem_ReturnsTrue()
    {
        // Arrange
        _historyService.AddToHistory("resto1");

        // Act
        var result = _historyService.IsInHistory("resto1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInHistory_NonExistingItem_ReturnsFalse()
    {
        // Act
        var result = _historyService.IsInHistory("resto1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInHistory_AfterRemove_ReturnsFalse()
    {
        // Arrange
        _historyService.AddToHistory("resto1");
        _historyService.RemoveFromHistory("resto1");

        // Act
        var result = _historyService.IsInHistory("resto1");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region MaxHistoryLimit Tests

    [Fact]
    public void AddToHistory_ExceedsMaxLimit_RemovesOldest()
    {
        // Act - Add more than 50 items
        for (int i = 0; i < 60; i++)
        {
            _historyService.AddToHistory($"resto{i}");
        }

        // Assert
        var result = _historyService.GetHistory();
        Assert.Equal(50, result.Count);
        Assert.Equal("resto59", result[0]); // Most recent
    }

    #endregion
}

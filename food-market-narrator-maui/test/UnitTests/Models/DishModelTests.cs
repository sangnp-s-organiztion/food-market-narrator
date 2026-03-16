using food_market_narrator.Models;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Models;

/// <summary>
/// Unit Tests cho DishModel
/// </summary>
public class DishModelTests
{
    [Fact]
    public void DishModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dish = new DishModel();

        // Assert
        Assert.Equal(0, dish.DishId);
        Assert.Equal(string.Empty, dish.Name);
        Assert.Null(dish.Price);
        Assert.Null(dish.Description);
        Assert.Equal(string.Empty, dish.RestaurantId);
        Assert.Null(dish.ImageId);
        Assert.Null(dish.CreatedAt);
    }

    [Fact]
    public void DishModel_CanSetProperties()
    {
        // Arrange & Act
        var dish = new DishModel
        {
            DishId = 1,
            Name = "Phở Bò",
            Price = 50000m,
            Description = "Phở bò truyền thống",
            RestaurantId = "restaurant-1",
            ImageId = 1,
            CreatedAt = new DateTime(2024, 1, 1)
        };

        // Assert
        Assert.Equal(1, dish.DishId);
        Assert.Equal("Phở Bò", dish.Name);
        Assert.Equal(50000m, dish.Price);
        Assert.Equal("Phở bò truyền thống", dish.Description);
        Assert.Equal("restaurant-1", dish.RestaurantId);
        Assert.Equal(1, dish.ImageId);
        Assert.Equal(new DateTime(2024, 1, 1), dish.CreatedAt);
    }

    [Fact]
    public void DishModel_PriceCanBeNull()
    {
        // Arrange & Act
        var dish = new DishModel
        {
            Name = "Combo",
            Price = null
        };

        // Assert
        Assert.Null(dish.Price);
    }

    [Fact]
    public void DishModel_PriceCanHaveValue()
    {
        // Arrange & Act
        var dish = new DishModel
        {
            Name = "Phở",
            Price = 50000m
        };

        // Assert
        Assert.NotNull(dish.Price);
        Assert.Equal(50000m, dish.Price);
    }
}

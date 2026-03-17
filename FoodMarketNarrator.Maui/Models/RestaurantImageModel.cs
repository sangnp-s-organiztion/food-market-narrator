namespace food_market_narrator.Models;

public class RestaurantImageModel
{
    public int ImageId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

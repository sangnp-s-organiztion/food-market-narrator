namespace food_market_narrator_api.DTOs.Dish
{
    public class DishResponse
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string RestaurantId { get; set; } = string.Empty;
        public int? ImageId { get; set; }
        public string? ImageFileName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

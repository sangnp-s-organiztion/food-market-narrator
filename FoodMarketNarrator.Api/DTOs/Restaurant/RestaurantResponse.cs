namespace food_market_narrator_api.DTOs.Restaurant
{
    public class RestaurantResponse
    {
        public string RestaurantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<RestaurantImageResponse> Images { get; set; } = new();
        public List<AudioResponse> Audios { get; set; } = new();
    }

    public class RestaurantImageResponse
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class AudioResponse
    {
        public int AudioId { get; set; }
        public int LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateGeneration { get; set; }
    }
}

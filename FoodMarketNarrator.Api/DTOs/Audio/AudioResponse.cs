using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Audio
{
    public class AudioResponse
    {
        public int AudioId { get; set; }
        public string RestaurantId { get; set; }
        public int LanguageId { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateGeneration { get; set; }

    }
}
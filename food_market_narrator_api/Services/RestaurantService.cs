using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class RestaurantService
    {
        private readonly RestaurantRepository _restaurantRepository;
        public RestaurantService(RestaurantRepository repository)
        {
            _restaurantRepository = repository;
        }
        public async Task<List<RestaurantResponseDto>> GetAllRestaurantsAsync()
        {
            var restaurants = await _restaurantRepository.GetAllAsync();

            return restaurants.Select(r => new RestaurantResponseDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                Images = r.ImageURL
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new RestaurantImageResponseDto
                    {
                        ImageId = i.ImageId,
                        ImageUrl = i.ImageUrl,
                        IsPrimary = i.IsPrimary,
                        SortOrder = i.SortOrder
                    })
                    .ToList(),
                Audios = r.AudioURL
                    .OrderBy(a => a.LanguageId)
                    .ThenBy(a => a.Version)
                    .Select(a => new AudioResponseDto
                    {
                        AudioId = a.AudioId,
                        LanguageId = a.LanguageId,
                        LanguageName = a.Language?.LanguageName ?? string.Empty,
                        LanguageCode = a.Language?.LanguageCode ?? string.Empty,
                        AudioUrl = a.AudioUrl,
                        Version = a.Version,
                        IsActive = a.IsActive,
                        DateGeneration = a.DateGeneration
                    })
                    .ToList()
            }).ToList();
        }
    }
}

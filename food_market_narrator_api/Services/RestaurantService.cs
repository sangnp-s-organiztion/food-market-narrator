using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class RestaurantService
    {
        private readonly RestaurantRepository _restaurantRepository;

        public RestaurantService(RestaurantRepository restaurantRepository)
        {
            _restaurantRepository = restaurantRepository;
        }

        public async Task<List<RestaurantResponseDto>> GetAllRestaurantsAsync()
        {
            var restaurants = await _restaurantRepository.GetAllAsync();
            return restaurants.Select(MapToDto).ToList();
        }

        public async Task<RestaurantResponseDto?> GetRestaurantByIdAsync(string id)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(id);
            if (restaurant == null) return null;
            return MapToDto(restaurant);
        }

        public async Task<RestaurantResponseDto?> UpdateRestaurantAsync(string id, DTOs.Restaurant.RestaurantRequestDto dto)
        {
            var updated = await _restaurantRepository.UpdateAsync(id, dto);
            if (updated == null) return null;
            return MapToDto(updated);
        }

        public async Task<RestaurantResponseDto?> SetActiveAsync(string id, bool isActive)
        {
            var updated = await _restaurantRepository.SetActiveAsync(id, isActive);
            if (updated == null) return null;
            return MapToDto(updated);
        }

        public async Task<List<RestaurantResponseDto>> GetRestaurantsByUserIdAsync(int userId)
        {
            var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
            return restaurants.Select(MapToDto).ToList();
        }

        private static RestaurantResponseDto MapToDto(RestaurantModel r)
        {
            return new RestaurantResponseDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                Phone = r.Phone,
                OpenTime = r.OpenTime,
                CloseTime = r.CloseTime,
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
            };
        }
    }
}

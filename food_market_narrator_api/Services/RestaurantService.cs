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

            return restaurants.Select(MapRestaurant).ToList();
        }
    
        public async Task<RestaurantResponseDto> GetRestaurantByIdAsync(string id)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(id);

            if (restaurant == null)
                return null;

            return MapRestaurant(restaurant);
        }

        public async Task<List<RestaurantResponseDto>> GetRestaurantsByUserIdAsync(int userId)
        {
            var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
            return restaurants.Select(MapRestaurant).ToList();
        }

        public async Task<RestaurantResponseDto?> UpdateRestaurantAsync(string restaurantId, UpdateRestaurantRequestDto request)
        {
            var existing = await _restaurantRepository.GetByIdAsync(restaurantId);
            if (existing == null)
            {
                return null;
            }

            existing.Name = request.Name.Trim();
            existing.Description = request.Description;
            existing.Phone = request.Phone;
            existing.Address = request.Address;
            existing.Latitude = request.Latitude;
            existing.Longitude = request.Longitude;
            existing.OpenTime = request.OpenTime;
            existing.CloseTime = request.CloseTime;

            bool updated = await _restaurantRepository.UpdateAsync(existing);
            if (!updated)
            {
                return null;
            }

            var latest = await _restaurantRepository.GetByIdAsync(restaurantId);
            return latest == null ? null : MapRestaurant(latest);
        }

        public async Task<bool> UpdateRestaurantStatusAsync(string restaurantId, bool isActive)
        {
            return await _restaurantRepository.UpdateStatusAsync(restaurantId, isActive);
        }

        public async Task<List<RestaurantImageResponseDto>> GetImagesByRestaurantIdAsync(string restaurantId)
        {
            var images = await _restaurantRepository.GetImagesByRestaurantIdAsync(restaurantId);
            return images.Select(i => new RestaurantImageResponseDto
            {
                ImageId = i.ImageId,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList();
        }

        public async Task<RestaurantImageResponseDto> AddImageAsync(string restaurantId, string imageUrl, bool isPrimary, int sortOrder)
        {
            var created = await _restaurantRepository.AddImageAsync(new RestaurantImageModel
            {
                RestaurantId = restaurantId,
                ImageUrl = imageUrl,
                IsPrimary = isPrimary,
                SortOrder = sortOrder
            });

            return new RestaurantImageResponseDto
            {
                ImageId = created.ImageId,
                ImageUrl = created.ImageUrl,
                IsPrimary = created.IsPrimary,
                SortOrder = created.SortOrder
            };
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            return await _restaurantRepository.DeleteImageAsync(imageId);
        }

        public async Task<bool> SetPrimaryImageAsync(int imageId, bool isPrimary)
        {
            return await _restaurantRepository.SetPrimaryImageAsync(imageId, isPrimary);
        }

        public async Task<bool> ReorderImagesAsync(string restaurantId, List<ReorderImageItemDto> items)
        {
            var mappedItems = items.Select(i => (i.ImageId, i.SortOrder)).ToList();
            return await _restaurantRepository.ReorderImagesAsync(restaurantId, mappedItems);
        }

        private static RestaurantResponseDto MapRestaurant(RestaurantModel r)
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
                IsActive = r.IsActive,
                UserId = r.UserId,
                OpenTime = r.OpenTime,
                CloseTime = r.CloseTime,
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

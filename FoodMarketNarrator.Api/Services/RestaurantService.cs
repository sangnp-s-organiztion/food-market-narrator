using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class RestaurantService
    {
        private readonly RestaurantRepository _restaurantRepository;
        private readonly TranslationService _translationService;

        public RestaurantService(RestaurantRepository repository, TranslationService translationService)
        {
            _restaurantRepository = repository;
            _translationService = translationService;
        }
        public async Task<List<RestaurantResponse>> GetAllRestaurantsAsync()
        {
            var restaurants = await _restaurantRepository.GetAllAsync();

            return restaurants.Select(MapRestaurant).ToList();
        }

        public async Task<int> CountRestaurantsAsync()
        {
            return await _restaurantRepository.CountAsync();
        }
    
        public async Task<RestaurantResponse> GetRestaurantByIdAsync(string id)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(id);

            if (restaurant == null)
                return null;

            return MapRestaurant(restaurant);
        }

        public async Task<List<RestaurantResponse>> GetRestaurantsByUserIdAsync(int userId)
        {
            var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
            return restaurants.Select(MapRestaurant).ToList();
        }

        public async Task<RestaurantResponse> CreateRestaurantAsync(CreateRestaurantRequest request)
        {
            var now = DateTime.UtcNow;
            var model = new RestaurantModel
            {
                RestaurantId = $"rst_{Guid.NewGuid():N}",
                Name = request.Name.Trim(),
                Description = request.Description,
                Phone = request.Phone,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                IsActive = request.IsActive,
                UserId = request.UserId,
                CreatedAt = now
            };

            var created = await _restaurantRepository.AddAsync(model);

            if (created.UserId > 0)
            {
                await _translationService.SyncRestaurantInfoTranslationsAsync(
                    created.UserId,
                    created.RestaurantId,
                    new RestaurantTranslationContent
                    {
                        Name = created.Name,
                        Description = created.Description,
                        Address = created.Address
                    },
                    fieldsToSync: new[] { "description", "address" },
                    requestIdPrefix: $"restaurant-create-{created.RestaurantId}");
            }

            return MapRestaurant(created);
        }

        public async Task<RestaurantResponse?> UpdateRestaurantAsync(
            string restaurantId,
            UpdateRestaurantRequest request,
            int? sellerUserId = null)
        {
            var existing = await _restaurantRepository.GetByIdAsync(restaurantId);
            if (existing == null)
            {
                return null;
            }

            var changedTranslationFields = new List<string>();

            if (!string.Equals(NormalizeForComparison(existing.Description), NormalizeForComparison(request.Description), StringComparison.Ordinal))
            {
                changedTranslationFields.Add("description");
            }

            if (!string.Equals(NormalizeForComparison(existing.Address), NormalizeForComparison(request.Address), StringComparison.Ordinal))
            {
                changedTranslationFields.Add("address");
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

            if (sellerUserId.HasValue && changedTranslationFields.Count > 0)
            {
                await _translationService.SyncRestaurantInfoTranslationsAsync(
                    sellerUserId.Value,
                    restaurantId,
                    new RestaurantTranslationContent
                    {
                        Name = existing.Name,
                        Description = existing.Description,
                        Address = existing.Address
                    },
                    fieldsToSync: changedTranslationFields,
                    requestIdPrefix: $"restaurant-update-{restaurantId}");
            }

            var latest = await _restaurantRepository.GetByIdAsync(restaurantId);
            return latest == null ? null : MapRestaurant(latest);
        }

        private static string NormalizeForComparison(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        public async Task<bool> UpdateRestaurantStatusAsync(string restaurantId, bool isActive)
        {
            return await _restaurantRepository.UpdateStatusAsync(restaurantId, isActive);
        }

        public async Task<List<RestaurantImageResponse>> GetImagesByRestaurantIdAsync(string restaurantId)
        {
            var images = await _restaurantRepository.GetImagesByRestaurantIdAsync(restaurantId);
            return images.Select(i => new RestaurantImageResponse
            {
                ImageId = i.ImageId,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList();
        }

        public async Task<RestaurantImageResponse> AddImageAsync(string restaurantId, string imageUrl, bool isPrimary, int sortOrder)
        {
            var created = await _restaurantRepository.AddImageAsync(new RestaurantImageModel
            {
                RestaurantId = restaurantId,
                ImageUrl = imageUrl,
                IsPrimary = isPrimary,
                SortOrder = sortOrder
            });

            return new RestaurantImageResponse
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

        public async Task<RestaurantImageResponse?> GetImageByIdAsync(int imageId)
        {
            var image = await _restaurantRepository.GetImageByIdAsync(imageId);
            if (image == null)
            {
                return null;
            }

            return new RestaurantImageResponse
            {
                ImageId = image.ImageId,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder,
            };
        }

        public async Task<bool> SetPrimaryImageAsync(int imageId, bool isPrimary)
        {
            return await _restaurantRepository.SetPrimaryImageAsync(imageId, isPrimary);
        }

        public async Task<bool> ReorderImagesAsync(string restaurantId, List<ReorderImageItem> items)
        {
            var mappedItems = items.Select(i => (i.ImageId, i.SortOrder)).ToList();
            return await _restaurantRepository.ReorderImagesAsync(restaurantId, mappedItems);
        }

        private static RestaurantResponse MapRestaurant(RestaurantModel r)
        {
            return new RestaurantResponse
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
                    .Select(i => new RestaurantImageResponse
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
                    .Select(a => new AudioResponse
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

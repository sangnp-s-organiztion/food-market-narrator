using food_market_narrator_api.DTOs.Audio;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class AudioService
    {
        private readonly AudioRepository _audioRepository;
        public AudioService(AudioRepository repository)
        {
            _audioRepository = repository;
        }
        public async Task<List<AudioResponse>> GetAllAudiosAsync()
        {
            var audios = await _audioRepository.GetAllAsync();

            return audios.Select(a => new AudioResponse
            {
                AudioId = a.AudioId,
                RestaurantId = a.RestaurantId,
                LanguageId = a.LanguageId,
                AudioUrl = a.AudioUrl,
                Version = a.Version,
                IsActive = a.IsActive,
                DateGeneration = a.DateGeneration
            }).ToList();
        }

        public async Task<List<AudioResponse>> GetByRestaurantIdAsync(string restaurantId)
        {
            var audios = await _audioRepository.GetByRestaurantIdAsync(restaurantId);
            return audios.Select(a => new AudioResponse
            {
                AudioId = a.AudioId,
                RestaurantId = a.RestaurantId,
                LanguageId = a.LanguageId,
                AudioUrl = a.AudioUrl,
                Version = a.Version,
                IsActive = a.IsActive,
                DateGeneration = a.DateGeneration
            }).ToList();
        }

        public async Task<AudioResponse> CreateAsync(string restaurantId, int languageId, string audioUrl)
        {
            var created = await _audioRepository.CreateAsync(new AudioModel
            {
                RestaurantId = restaurantId,
                LanguageId = languageId,
                AudioUrl = audioUrl,
                Version = 1,
                IsActive = true,
                DateGeneration = DateTime.UtcNow
            });

            return new AudioResponse
            {
                AudioId = created.AudioId,
                RestaurantId = created.RestaurantId,
                LanguageId = created.LanguageId,
                AudioUrl = created.AudioUrl,
                Version = created.Version,
                IsActive = created.IsActive,
                DateGeneration = created.DateGeneration
            };
        }

        public async Task<bool> UpdateActiveAsync(int audioId, bool isActive)
        {
            return await _audioRepository.UpdateActiveAsync(audioId, isActive);
        }

        public async Task<bool> DeleteAsync(int audioId)
        {
            return await _audioRepository.DeleteAsync(audioId);
        }
    }
}
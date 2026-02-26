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
    }
}
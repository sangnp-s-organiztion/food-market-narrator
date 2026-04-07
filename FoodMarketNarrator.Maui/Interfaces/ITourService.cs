using food_market_narrator.Models;

namespace food_market_narrator.Services;

public interface ITourService
{
    Task<List<TourModel>> GetToursAsync();
    Task<TourModel?> GetTourByIdAsync(int tourId);
}

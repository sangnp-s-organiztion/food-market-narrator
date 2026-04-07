using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories;

public class TourRepository
{
    private readonly AppDbContext _context;

    public TourRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TourModel>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Tour
            .Include(t => t.Image)
            .Include(t => t.TourRestaurants)
                .ThenInclude(tr => tr.Restaurant)
                    .ThenInclude(r => r.ImageURL)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.ToListAsync();
    }

    public async Task<TourModel?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var query = _context.Tour
            .Include(t => t.Image)
            .Include(t => t.TourRestaurants)
                .ThenInclude(tr => tr.Restaurant)
                    .ThenInclude(r => r.ImageURL)
            .Where(t => t.TourId == id)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query.FirstOrDefaultAsync();
    }

    public Task<bool> ExistsAsync(int id, bool includeInactive = false)
    {
        var query = _context.Tour.Where(t => t.TourId == id).AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return query.AnyAsync();
    }

    public Task<bool> RestaurantExistsAsync(string restaurantId)
    {
        return _context.Restaurant.AnyAsync(r => r.RestaurantId == restaurantId);
    }

    public Task<bool> TourRestaurantExistsAsync(int tourId, string restaurantId)
    {
        return _context.TourRestaurant.AnyAsync(tr => tr.TourId == tourId && tr.RestaurantId == restaurantId);
    }

    public async Task<int> GetNextStopOrderAsync(int tourId)
    {
        var maxStopOrder = await _context.TourRestaurant
            .Where(tr => tr.TourId == tourId)
            .Select(tr => (int?)tr.StopOrder)
            .MaxAsync();

        return (maxStopOrder ?? 0) + 1;
    }

    public async Task AddRestaurantToTourAsync(int tourId, string restaurantId, int stopOrder)
    {
        var entity = new TourRestaurantModel
        {
            TourId = tourId,
            RestaurantId = restaurantId,
            StopOrder = stopOrder,
            CreatedAt = DateTime.UtcNow
        };

        _context.TourRestaurant.Add(entity);
        await _context.SaveChangesAsync();
    }
}

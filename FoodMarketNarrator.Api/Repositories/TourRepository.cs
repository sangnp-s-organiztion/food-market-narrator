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

    public Task<List<string>> GetTourRestaurantIdsAsync(int tourId)
    {
        return _context.TourRestaurant
            .Where(tr => tr.TourId == tourId)
            .Select(tr => tr.RestaurantId)
            .ToListAsync();
    }

    public async Task ReorderStopsAsync(int tourId, IReadOnlyList<string> orderedRestaurantIds)
    {
        var stopOrderMap = orderedRestaurantIds
            .Select((restaurantId, index) => new { restaurantId, stopOrder = index + 1 })
            .ToDictionary(x => x.restaurantId, x => x.stopOrder, StringComparer.OrdinalIgnoreCase);

        var entities = await _context.TourRestaurant
            .Where(tr => tr.TourId == tourId)
            .ToListAsync();

        // 2-phase update prevents transient unique-key conflicts on (tour_id, stop_order).
        using var transaction = await _context.Database.BeginTransactionAsync();

        foreach (var entity in entities)
        {
            if (stopOrderMap.TryGetValue(entity.RestaurantId, out var newOrder))
            {
                entity.StopOrder = -newOrder;
            }
        }

        await _context.SaveChangesAsync();

        foreach (var entity in entities)
        {
            if (stopOrderMap.TryGetValue(entity.RestaurantId, out var newOrder))
            {
                entity.StopOrder = newOrder;
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> UpdateTourMetadataAsync(
        int tourId,
        int? estimatedDurationMinutes,
        int sortPriority,
        bool isActive,
        bool isFeatured)
    {
        var tour = await _context.Tour.FirstOrDefaultAsync(t => t.TourId == tourId);
        if (tour == null)
        {
            return false;
        }

        tour.EstimatedDurationMinutes = estimatedDurationMinutes;
        tour.SortPriority = sortPriority;
        tour.IsActive = isActive;
        tour.IsFeatured = isFeatured;
        tour.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TourModel> CreateTourAsync(
        string name,
        string? shortDescription,
        string? description,
        int? estimatedDurationMinutes,
        bool isActive,
        bool isFeatured,
        int sortPriority)
    {
        var entity = new TourModel
        {
            Name = name,
            ShortDescription = shortDescription,
            Description = description,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            IsActive = isActive,
            IsFeatured = isFeatured,
            SortPriority = sortPriority,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tour.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}

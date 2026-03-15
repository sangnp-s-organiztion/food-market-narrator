using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories
{
    public class UserRestaurantRepository
    {
        private readonly AppDbContext _context;
        public UserRestaurantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetRestaurantIdsByUserAsync(int userId)
        {
            return await _context.UserRestaurant
                .Where(ur => ur.UserId == userId)
                .OrderBy(ur => ur.Id)
                .Select(ur => ur.RestaurantId)
                .ToListAsync();
        }
    }
}

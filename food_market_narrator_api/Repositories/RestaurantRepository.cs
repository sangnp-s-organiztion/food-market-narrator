using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;


namespace food_market_narrator_api.Repositories
{
    public class RestaurantRepository
    {
        private readonly AppDbContext _context;
        public RestaurantRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<RestaurantModel>> GetAllAsync()
        {
            return await _context.Restaurant.ToListAsync();
        }
    }
}

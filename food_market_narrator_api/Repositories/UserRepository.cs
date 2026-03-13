using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // get all users
        public async Task<List<UserModel>> GetAllAsync()
        {
            return await _context.User.ToListAsync();
        }

        // get user by id
        public async Task<UserModel> GetByIdAsync(int id)
        {
            // FindAsync sẽ trả về null nếu không tìm thấy
            // FindAsync tối ưu cho search theo PK
            return await _context.User.FindAsync(id);
        }
    }
}
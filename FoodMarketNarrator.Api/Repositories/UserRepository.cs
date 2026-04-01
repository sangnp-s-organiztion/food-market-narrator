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

        public async Task<int> CountAsync()
        {
            return await _context.User.CountAsync();
        }

        // get user by id
        public async Task<UserModel> GetByIdAsync(int id)
        {
            // FindAsync sẽ trả về null nếu không tìm thấy
            // FindAsync tối ưu cho search theo PK
            return await _context.User.FindAsync(id);
        }

        public async Task<UserModel?> GetByUsernameAsync(string username)
        {
            return await _context.User
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string passwordHash)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            // Current DB stores password_hash; compare directly until hash verification is implemented.
            return string.Equals(user.Password, passwordHash, StringComparison.Ordinal);
        }

        public async Task<UserModel> CreateAsync(UserModel user)
        {
            _context.User.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateRoleAsync(int userId, string role)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;
            user.Role = role;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int userId, bool isActive)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;
            user.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
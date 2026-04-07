using food_market_narrator_api.Models;
using food_market_narrator_api.Helpers;
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

        public async Task<List<UserModel>> GetByIdsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return new List<UserModel>();
            }

            return await _context.User
                .Where(u => ids.Contains(u.UserId))
                .ToListAsync();
        }

        public async Task<UserModel?> GetByUsernameAsync(string username)
        {
            return await _context.User
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            if (PasswordHasher.IsHashed(user.Password))
            {
                return PasswordHasher.Verify(password, user.Password);
            }

            // Legacy fallback: support existing plaintext rows and migrate immediately on successful login.
            if (!string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                return false;
            }

            user.Password = PasswordHasher.Hash(password);
            await _context.SaveChangesAsync();
            return true;
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

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;
            user.Password = passwordHash;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string username, string phone, string email)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;

            user.Username = username;
            user.Phone = phone;
            user.Email = email;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

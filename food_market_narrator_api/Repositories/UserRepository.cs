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

        public async Task<UserModel?> GetByUsernameAsync(string username)
        {
            return await _context.Set<UserModel>()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string passwordHash)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null) return false;
            // For now compare hashes directly. In future use secure hashing verification.
            return string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal);
        }
    }
}

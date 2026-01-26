using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class VerificationTokenRepository : Repository<VerificationToken>, IVerificationTokenRepository
    {
        public VerificationTokenRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<VerificationToken?> GetValidTokenAsync(string token, string email, TokenType type)
        {
            return await _dbSet
                .Include(vt => vt.User)
                .FirstOrDefaultAsync(vt => 
                    vt.Token == token && 
                    vt.User != null && 
                    vt.User.Email == email &&
                    vt.Type == type &&
                    !vt.IsUsed && 
                    vt.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<VerificationToken?> GetLatestTokenByUserIdAsync(long userId, TokenType type)
        {
            return await _dbSet
                .Where(vt => vt.UserId == userId && vt.Type == type && !vt.IsUsed)
                .OrderByDescending(vt => vt.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidateAllTokensForUserAsync(long userId, TokenType type)
        {
            var tokens = await _dbSet
                .Where(vt => vt.UserId == userId && vt.Type == type && !vt.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
            }
        }
    }
}

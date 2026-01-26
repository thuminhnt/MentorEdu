using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IVerificationTokenRepository : IRepository<VerificationToken>
    {
        Task<VerificationToken?> GetValidTokenAsync(string token, string email, TokenType type);
        Task<VerificationToken?> GetLatestTokenByUserIdAsync(long userId, TokenType type);
        Task InvalidateAllTokensForUserAsync(long userId, TokenType type);
    }
}

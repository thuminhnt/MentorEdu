using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IWalletRepository : IRepository<Wallet>
    {
        Task<Wallet?> GetByUserIdAsync(long userId);
        Task<Wallet?> GetByUserIdWithTransactionsAsync(long userId);
        Task<decimal> GetBalanceByUserIdAsync(long userId);
        Task<bool> HasSufficientBalanceAsync(long userId, decimal amount);
    }
}

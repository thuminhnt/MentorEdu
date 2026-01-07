using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class WalletRepository : Repository<Wallet>, IWalletRepository
    {
        public WalletRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Wallet?> GetByUserIdAsync(long userId)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<Wallet?> GetByUserIdWithTransactionsAsync(long userId)
        {
            return await _dbSet
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(20))
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<decimal> GetBalanceByUserIdAsync(long userId)
        {
            var wallet = await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId);
            return wallet?.Balance ?? 0;
        }

        public async Task<bool> HasSufficientBalanceAsync(long userId, decimal amount)
        {
            var wallet = await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId);
            return wallet != null && wallet.Balance >= amount;
        }
    }
}

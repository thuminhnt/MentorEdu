using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(long walletId)
        {
            return await _dbSet
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(t => t.OrderId == orderId)
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByTypeAsync(TransactionType type)
        {
            return await _dbSet
                .Where(t => t.Type == type)
                .Include(t => t.Wallet)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByWalletAsync(
            long walletId,
            int pageNumber,
            int pageSize,
            TransactionType? type = null)
        {
            var query = _dbSet.Where(t => t.WalletId == walletId);

            if (type.HasValue)
                query = query.Where(t => t.Type == type);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(t => t.Order)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}

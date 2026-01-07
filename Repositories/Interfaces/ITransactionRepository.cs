using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByWalletIdAsync(long walletId);
        Task<IEnumerable<Transaction>> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<Transaction>> GetByTypeAsync(TransactionType type);
        Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByWalletAsync(
            long walletId,
            int pageNumber,
            int pageSize,
            TransactionType? type = null);
    }
}

using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IEscrowRepository : IRepository<Escrow>
    {
        Task<Escrow?> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<Escrow>> GetByStatusAsync(EscrowStatus status);
        Task<decimal> GetTotalHoldingAmountAsync();
    }
}

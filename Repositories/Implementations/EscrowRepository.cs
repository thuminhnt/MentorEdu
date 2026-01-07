using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class EscrowRepository : Repository<Escrow>, IEscrowRepository
    {
        public EscrowRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Escrow?> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Include(e => e.Order)
                .FirstOrDefaultAsync(e => e.OrderId == orderId);
        }

        public async Task<IEnumerable<Escrow>> GetByStatusAsync(EscrowStatus status)
        {
            return await _dbSet
                .Where(e => e.Status == status)
                .Include(e => e.Order)
                    .ThenInclude(o => o.Student)
                .Include(e => e.Order)
                    .ThenInclude(o => o.Mentor)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalHoldingAmountAsync()
        {
            return await _dbSet
                .Where(e => e.Status == EscrowStatus.Holding)
                .SumAsync(e => e.Amount);
        }
    }
}

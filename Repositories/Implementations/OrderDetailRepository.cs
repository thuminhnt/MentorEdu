using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        public OrderDetailRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(od => od.OrderId == orderId)
                .Include(od => od.ChangedByUser)
                .OrderByDescending(od => od.CreatedAt)
                .ToListAsync();
        }

        public async Task<OrderDetail?> GetLatestByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(od => od.OrderId == orderId)
                .Include(od => od.ChangedByUser)
                .OrderByDescending(od => od.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}

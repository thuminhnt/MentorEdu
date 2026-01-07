using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(long orderId);
        Task<OrderDetail?> GetLatestByOrderIdAsync(long orderId);
    }
}

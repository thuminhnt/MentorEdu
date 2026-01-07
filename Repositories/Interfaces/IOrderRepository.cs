using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(long id);
        Task<IEnumerable<Order>> GetByStudentIdAsync(long studentId);
        Task<IEnumerable<Order>> GetByMentorIdAsync(long mentorId);
        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
        Task<IEnumerable<Order>> GetActiveOrdersAsync();
        Task<IEnumerable<Order>> GetCompletedOrdersAsync();
        Task<IEnumerable<Order>> GetDisputedOrdersAsync();
        Task<decimal> GetTotalRevenueByMentorAsync(long mentorId);
        Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedByUserAsync(
            long userId,
            bool isMentor,
            int pageNumber,
            int pageSize,
            OrderStatus? status = null);
    }
}

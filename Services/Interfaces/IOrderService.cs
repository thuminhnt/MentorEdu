using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Order;
using MentorEdu.DTOs.Review;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IOrderService
    {
        // CRUD đơn hàng
        Task<ServiceResult<OrderDetailDto>> GetByIdAsync(long orderId, long userId);
        Task<ServiceResult<PagedResponse<OrderListDto>>> GetMyOrdersAsync(long userId, bool isMentor, SearchOrderDto searchDto);
        Task<ServiceResult> UpdateOrderStatusAsync(long orderId, long userId, UpdateOrderStatusDto request);
        Task<ServiceResult> CancelOrderAsync(long orderId, long userId, string? reason = null);
        Task<ServiceResult> CompleteOrderAsync(long orderId, long userId);
        Task<ServiceResult> DisputeOrderAsync(long orderId, long userId, string reason);
        
        // Phiên làm việc
        Task<ServiceResult<WorkSessionDetailDto>> StartWorkSessionAsync(long userId, StartWorkSessionDto request);
        Task<ServiceResult<WorkSessionDetailDto>> EndWorkSessionAsync(long userId, EndWorkSessionDto request);
        Task<ServiceResult<WorkSessionDetailDto>> ResumeWorkSessionAsync(long userId, ResumeWorkSessionDto request);
        Task<ServiceResult<WorkSessionSummaryDto>> GetWorkSessionSummaryAsync(long orderId);
        
        // Đánh giá
        Task<ServiceResult<ReviewDto>> CreateReviewAsync(long userId, CreateReviewDto request);
        Task<ServiceResult<List<ReviewDto>>> GetOrderReviewsAsync(long orderId);
    }
}

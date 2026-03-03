using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminOrderService
    {
        // Yêu cầu
        Task<ServiceResult<PagedResponse<AdminRequestListDto>>> GetAllRequestsAsync(AdminSearchRequestDto request);
        Task<ServiceResult> AdminDeleteRequestAsync(long requestId, string reason);

        // Đề xuất
        Task<ServiceResult<PagedResponse<AdminProposalListDto>>> GetAllProposalsAsync(AdminSearchProposalDto request);
        Task<ServiceResult> AdminDeleteProposalAsync(long proposalId, string reason);

        // Đơn hàng
        Task<ServiceResult<PagedResponse<AdminOrderListDto>>> GetAllOrdersAsync(AdminSearchOrderDto request);
        Task<ServiceResult<AdminOrderListDto>> GetOrderDetailForAdminAsync(long orderId);
        Task<ServiceResult> AdminUpdateOrderStatusAsync(long orderId, long adminId, AdminUpdateOrderStatusDto request);
        Task<ServiceResult> AdminRefundOrderAsync(long orderId, long adminId, AdminRefundOrderDto request);

        // Giao dịch
        Task<ServiceResult<PagedResponse<AdminTransactionListDto>>> GetAllTransactionsAsync(AdminSearchTransactionDto request);
        Task<ServiceResult> AdminVerifyTransactionAsync(long transactionId, long adminId, bool approve, string? note);

        // Ví tiền
        Task<ServiceResult<PagedResponse<AdminWalletListDto>>> GetAllWalletsAsync(AdminSearchWalletDto request);
        Task<ServiceResult<AdminWalletListDto>> GetWalletDetailForAdminAsync(long walletId);

        // Đánh giá
        Task<ServiceResult<PagedResponse<AdminReviewListDto>>> GetAllReviewsAsync(AdminSearchReviewDto request);
        Task<ServiceResult> AdminDeleteReviewAsync(long reviewId, string reason);
    }
}

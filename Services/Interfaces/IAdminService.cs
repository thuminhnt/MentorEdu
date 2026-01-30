using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminService
    {
        // Dashboard Statistics
        Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync();

        #region User Management
        Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null);
        Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(long userId);
        Task<ServiceResult<UserDetailDto>> AdminCreateUserAsync(AdminCreateUserDto request);
        Task<ServiceResult<UserDetailDto>> AdminUpdateUserAsync(long userId, AdminUpdateUserDto request);
        Task<ServiceResult> AdminDeleteUserAsync(long userId);
        Task<ServiceResult> ActivateUserAsync(long userId);
        Task<ServiceResult> DeactivateUserAsync(long userId);
        Task<ServiceResult> BanUserAsync(long userId, string reason);
        Task<ServiceResult> VerifyUserAsync(long userId);
        #endregion

        #region Category Management
        Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync();
        Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId);
        Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request);
        Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request);
        Task<ServiceResult> DeleteCategoryAsync(long categoryId);
        #endregion

        #region Category-Hashtag Mapping
        Task<ServiceResult<List<CategoryHashtagMappingDto>>> GetCategoryHashtagMappingsAsync();
        Task<ServiceResult<CategoryHashtagDetailDto>> GetHashtagsByCategoryAsync(long categoryId);
        Task<ServiceResult> AddCategoryHashtagMappingAsync(AddCategoryHashtagDto request);
        Task<ServiceResult> RemoveCategoryHashtagMappingAsync(RemoveCategoryHashtagDto request);
        #endregion

        #region Report Management
        Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(int pageNumber, int pageSize, string? status = null);
        Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId);
        Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request);
        #endregion

        #region Requests Management
        Task<ServiceResult<PagedResponse<AdminRequestListDto>>> GetAllRequestsAsync(AdminSearchRequestDto request);
        Task<ServiceResult> AdminDeleteRequestAsync(long requestId, string reason);
        #endregion

        #region Proposals Management
        Task<ServiceResult<PagedResponse<AdminProposalListDto>>> GetAllProposalsAsync(AdminSearchProposalDto request);
        Task<ServiceResult> AdminDeleteProposalAsync(long proposalId, string reason);
        #endregion

        #region Orders Management
        Task<ServiceResult<PagedResponse<AdminOrderListDto>>> GetAllOrdersAsync(AdminSearchOrderDto request);
        Task<ServiceResult<AdminOrderListDto>> GetOrderDetailForAdminAsync(long orderId);
        Task<ServiceResult> AdminUpdateOrderStatusAsync(long orderId, long adminId, AdminUpdateOrderStatusDto request);
        Task<ServiceResult> AdminRefundOrderAsync(long orderId, long adminId, AdminRefundOrderDto request);
        #endregion

        #region Community Posts Management
        Task<ServiceResult<PagedResponse<AdminPostListDto>>> GetAllPostsAsync(AdminSearchPostDto request);
        Task<ServiceResult> AdminDeletePostAsync(long postId, string reason);
        Task<ServiceResult> AdminHidePostAsync(long postId, string reason);
        #endregion

        #region Community Comments Management
        Task<ServiceResult<PagedResponse<AdminCommentListDto>>> GetAllCommentsAsync(AdminSearchCommentDto request);
        Task<ServiceResult> AdminDeleteCommentAsync(long commentId, string reason);
        #endregion

        #region Transactions Management
        Task<ServiceResult<PagedResponse<AdminTransactionListDto>>> GetAllTransactionsAsync(AdminSearchTransactionDto request);
        Task<ServiceResult> AdminVerifyTransactionAsync(long transactionId, long adminId, bool approve, string? note);
        #endregion

        #region Wallets Management
        Task<ServiceResult<PagedResponse<AdminWalletListDto>>> GetAllWalletsAsync(AdminSearchWalletDto request);
        Task<ServiceResult<AdminWalletListDto>> GetWalletDetailForAdminAsync(long walletId);
        #endregion

        #region Reviews Management
        Task<ServiceResult<PagedResponse<AdminReviewListDto>>> GetAllReviewsAsync(AdminSearchReviewDto request);
        Task<ServiceResult> AdminDeleteReviewAsync(long reviewId, string reason);
        #endregion

        #region Hashtags Management
        Task<ServiceResult<PagedResponse<AdminHashtagDto>>> GetAllHashtagsAsync(AdminSearchHashtagDto request);
        Task<ServiceResult<List<HashtagDto>>> GetHashtagsAsync(); // For public autocomplete
        Task<ServiceResult<AdminHashtagDto>> CreateHashtagAsync(CreateHashtagDto request);
        Task<ServiceResult> DeleteHashtagAsync(long hashtagId);
        #endregion

        #region Mentor Applications Management
        Task<ServiceResult<PagedResponse<MentorApplicationDto>>> GetMentorApplicationsAsync(int pageNumber = 1, int pageSize = 10, MentorApprovalStatus? status = null);
        Task<ServiceResult<MentorApplicationDto>> GetMentorApplicationDetailAsync(long userId);
        Task<ServiceResult> ApproveMentorApplicationAsync(long userId, long adminId);
        Task<ServiceResult> RejectMentorApplicationAsync(long userId, long adminId, string reason);
        #endregion

        #region Mentor Application History
        Task<ServiceResult<PagedResponse<MentorApplicationHistoryDto>>> GetApplicationHistoryAsync(ApplicationHistoryQueryParams queryParams);
        Task<ServiceResult<List<MentorApplicationHistoryDto>>> GetUserApplicationHistoryAsync(long userId);
        Task<ServiceResult<List<MentorApplicationHistoryExportDto>>> ExportApplicationHistoryForMLAsync(DateTime? fromDate = null, DateTime? toDate = null, ApplicationAction? action = null);
        #endregion
    }

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalMentors { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int DisputedOrders { get; set; }
        public int PendingReports { get; set; }
        public int TotalRequests { get; set; }
        public int TotalProposals { get; set; }
        public int TotalPosts { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalEscrowHolding { get; set; }
        public List<RecentActivityDto>? RecentActivities { get; set; }
    }

    public class RecentActivityDto
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

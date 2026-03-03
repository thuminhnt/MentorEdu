using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    /// <summary>
    /// Lớp facade — ủy quyền toàn bộ phương thức IAdminService sang các sub-service chuyên biệt tương ứng.
    /// </summary>
    public class AdminService : BaseService, IAdminService
    {
        private readonly IAdminStatsService _statsService;
        private readonly IAdminUserService _userService;
        private readonly IAdminCategoryService _categoryService;
        private readonly IAdminReportService _reportService;
        private readonly IAdminOrderService _orderService;
        private readonly IAdminMentorReviewService _mentorReviewService;

        public AdminService(
            IUnitOfWork unitOfWork,
            IAdminStatsService statsService,
            IAdminUserService userService,
            IAdminCategoryService categoryService,
            IAdminReportService reportService,
            IAdminOrderService orderService,
            IAdminMentorReviewService mentorReviewService)
            : base(unitOfWork)
        {
            _statsService = statsService;
            _userService = userService;
            _categoryService = categoryService;
            _reportService = reportService;
            _orderService = orderService;
            _mentorReviewService = mentorReviewService;
        }

        // Bảng điều khiển tổng quan
        public Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync()
            => _statsService.GetDashboardAsync();

        // Quản lý người dùng
        public Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null)
            => _userService.GetUsersAsync(pageNumber, pageSize, role, isActive, keyword);
        public Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(long userId)
            => _userService.GetUserDetailAsync(userId);
        public Task<ServiceResult<UserDetailDto>> AdminCreateUserAsync(AdminCreateUserDto request)
            => _userService.AdminCreateUserAsync(request);
        public Task<ServiceResult<UserDetailDto>> AdminUpdateUserAsync(long userId, AdminUpdateUserDto request)
            => _userService.AdminUpdateUserAsync(userId, request);
        public Task<ServiceResult> AdminDeleteUserAsync(long userId)
            => _userService.AdminDeleteUserAsync(userId);
        public Task<ServiceResult> ActivateUserAsync(long userId)
            => _userService.ActivateUserAsync(userId);
        public Task<ServiceResult> DeactivateUserAsync(long userId)
            => _userService.DeactivateUserAsync(userId);
        public Task<ServiceResult> BanUserAsync(long userId, string reason)
            => _userService.BanUserAsync(userId, reason);
        public Task<ServiceResult> VerifyUserAsync(long userId)
            => _userService.VerifyUserAsync(userId);

        // Quản lý danh mục
        public Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync()
            => _categoryService.GetCategoriesAsync();
        public Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId)
            => _categoryService.GetCategoryByIdAsync(categoryId);
        public Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request)
            => _categoryService.CreateCategoryAsync(request);
        public Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request)
            => _categoryService.UpdateCategoryAsync(categoryId, request);
        public Task<ServiceResult> DeleteCategoryAsync(long categoryId)
            => _categoryService.DeleteCategoryAsync(categoryId);

        // Ánh xạ danh mục - hashtag
        public Task<ServiceResult<List<CategoryHashtagMappingDto>>> GetCategoryHashtagMappingsAsync()
            => _categoryService.GetCategoryHashtagMappingsAsync();
        public Task<ServiceResult<CategoryHashtagDetailDto>> GetHashtagsByCategoryAsync(long categoryId)
            => _categoryService.GetHashtagsByCategoryAsync(categoryId);
        public Task<ServiceResult> AddCategoryHashtagMappingAsync(AddCategoryHashtagDto request)
            => _categoryService.AddCategoryHashtagMappingAsync(request);
        public Task<ServiceResult> RemoveCategoryHashtagMappingAsync(RemoveCategoryHashtagDto request)
            => _categoryService.RemoveCategoryHashtagMappingAsync(request);

        // Quản lý báo cáo vi phạm
        public Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(int pageNumber, int pageSize, string? status = null)
            => _reportService.GetReportsAsync(pageNumber, pageSize, status);
        public Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId)
            => _reportService.GetReportDetailAsync(reportId);
        public Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request)
            => _reportService.HandleReportAsync(adminId, request);

        // Quản lý bài viết cộng đồng
        public Task<ServiceResult<PagedResponse<AdminPostListDto>>> GetAllPostsAsync(AdminSearchPostDto request)
            => _reportService.GetAllPostsAsync(request);
        public Task<ServiceResult> AdminDeletePostAsync(long postId, string reason)
            => _reportService.AdminDeletePostAsync(postId, reason);
        public Task<ServiceResult> AdminHidePostAsync(long postId, string reason)
            => _reportService.AdminHidePostAsync(postId, reason);
        public Task<ServiceResult> AdminUnhidePostAsync(long postId)
            => _reportService.AdminUnhidePostAsync(postId);

        // Quản lý bình luận cộng đồng
        public Task<ServiceResult<PagedResponse<AdminCommentListDto>>> GetAllCommentsAsync(AdminSearchCommentDto request)
            => _reportService.GetAllCommentsAsync(request);
        public Task<ServiceResult> AdminDeleteCommentAsync(long commentId, string reason)
            => _reportService.AdminDeleteCommentAsync(commentId, reason);

        // Quản lý yêu cầu tìm mentor
        public Task<ServiceResult<PagedResponse<AdminRequestListDto>>> GetAllRequestsAsync(AdminSearchRequestDto request)
            => _orderService.GetAllRequestsAsync(request);
        public Task<ServiceResult> AdminDeleteRequestAsync(long requestId, string reason)
            => _orderService.AdminDeleteRequestAsync(requestId, reason);

        // Quản lý đề xuất từ mentor
        public Task<ServiceResult<PagedResponse<AdminProposalListDto>>> GetAllProposalsAsync(AdminSearchProposalDto request)
            => _orderService.GetAllProposalsAsync(request);
        public Task<ServiceResult> AdminDeleteProposalAsync(long proposalId, string reason)
            => _orderService.AdminDeleteProposalAsync(proposalId, reason);

        // Quản lý đơn hàng
        public Task<ServiceResult<PagedResponse<AdminOrderListDto>>> GetAllOrdersAsync(AdminSearchOrderDto request)
            => _orderService.GetAllOrdersAsync(request);
        public Task<ServiceResult<AdminOrderListDto>> GetOrderDetailForAdminAsync(long orderId)
            => _orderService.GetOrderDetailForAdminAsync(orderId);
        public Task<ServiceResult> AdminUpdateOrderStatusAsync(long orderId, long adminId, AdminUpdateOrderStatusDto request)
            => _orderService.AdminUpdateOrderStatusAsync(orderId, adminId, request);
        public Task<ServiceResult> AdminRefundOrderAsync(long orderId, long adminId, AdminRefundOrderDto request)
            => _orderService.AdminRefundOrderAsync(orderId, adminId, request);

        // Quản lý giao dịch tài chính
        public Task<ServiceResult<PagedResponse<AdminTransactionListDto>>> GetAllTransactionsAsync(AdminSearchTransactionDto request)
            => _orderService.GetAllTransactionsAsync(request);
        public Task<ServiceResult> AdminVerifyTransactionAsync(long transactionId, long adminId, bool approve, string? note)
            => _orderService.AdminVerifyTransactionAsync(transactionId, adminId, approve, note);

        // Quản lý ví điện tử
        public Task<ServiceResult<PagedResponse<AdminWalletListDto>>> GetAllWalletsAsync(AdminSearchWalletDto request)
            => _orderService.GetAllWalletsAsync(request);
        public Task<ServiceResult<AdminWalletListDto>> GetWalletDetailForAdminAsync(long walletId)
            => _orderService.GetWalletDetailForAdminAsync(walletId);

        // Quản lý đánh giá
        public Task<ServiceResult<PagedResponse<AdminReviewListDto>>> GetAllReviewsAsync(AdminSearchReviewDto request)
            => _orderService.GetAllReviewsAsync(request);
        public Task<ServiceResult> AdminDeleteReviewAsync(long reviewId, string reason)
            => _orderService.AdminDeleteReviewAsync(reviewId, reason);

        // Quản lý hashtag
        public Task<ServiceResult<PagedResponse<AdminHashtagDto>>> GetAllHashtagsAsync(AdminSearchHashtagDto request)
            => _categoryService.GetAllHashtagsAsync(request);
        public Task<ServiceResult<List<HashtagDto>>> GetHashtagsAsync()
            => _categoryService.GetHashtagsAsync();
        public Task<ServiceResult<AdminHashtagDto>> CreateHashtagAsync(CreateHashtagDto request)
            => _categoryService.CreateHashtagAsync(request);
        public Task<ServiceResult> DeleteHashtagAsync(long hashtagId)
            => _categoryService.DeleteHashtagAsync(hashtagId);

        // Quản lý đơn đăng ký mentor
        public Task<ServiceResult<PagedResponse<MentorApplicationDto>>> GetMentorApplicationsAsync(int pageNumber = 1, int pageSize = 10, MentorApprovalStatus? status = null)
            => _mentorReviewService.GetMentorApplicationsAsync(pageNumber, pageSize, status);
        public Task<ServiceResult<MentorApplicationDto>> GetMentorApplicationDetailAsync(long userId)
            => _mentorReviewService.GetMentorApplicationDetailAsync(userId);
        public Task<ServiceResult> ApproveMentorApplicationAsync(long userId, long adminId)
            => _mentorReviewService.ApproveMentorApplicationAsync(userId, adminId);
        public Task<ServiceResult> RejectMentorApplicationAsync(long userId, long adminId, string reason)
            => _mentorReviewService.RejectMentorApplicationAsync(userId, adminId, reason);
        public Task<ServiceResult<MentorApplicationDto>> ReEvaluateMentorApplicationAsync(long userId, long adminId)
            => _mentorReviewService.ReEvaluateMentorApplicationAsync(userId, adminId);

        // Lịch sử đơn đăng ký mentor
        public Task<ServiceResult<PagedResponse<MentorApplicationHistoryDto>>> GetApplicationHistoryAsync(ApplicationHistoryQueryParams queryParams)
            => _mentorReviewService.GetApplicationHistoryAsync(queryParams);
        public Task<ServiceResult<List<MentorApplicationHistoryDto>>> GetUserApplicationHistoryAsync(long userId)
            => _mentorReviewService.GetUserApplicationHistoryAsync(userId);
        public Task<ServiceResult<List<MentorApplicationHistoryExportDto>>> ExportApplicationHistoryForMLAsync(DateTime? fromDate = null, DateTime? toDate = null, ApplicationAction? action = null)
            => _mentorReviewService.ExportApplicationHistoryForMLAsync(fromDate, toDate, action);
    }
}

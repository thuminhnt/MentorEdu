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
        // User Management
        Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null);
        Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(long userId);
        Task<ServiceResult> ActivateUserAsync(long userId);
        Task<ServiceResult> DeactivateUserAsync(long userId);
        Task<ServiceResult> BanUserAsync(long userId, string reason);
        Task<ServiceResult> VerifyUserAsync(long userId);
        
        // Category Management
        Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync();
        Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId);
        Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request);
        Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request);
        Task<ServiceResult> DeleteCategoryAsync(long categoryId);
        
        // Report Management
        Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(int pageNumber, int pageSize, string? status = null);
        Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId);
        Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request);
        
        // Dashboard Statistics
        Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync();
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

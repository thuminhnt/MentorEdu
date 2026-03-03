using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminMentorReviewService
    {
        // Đơn đăng ký mentor
        Task<ServiceResult<PagedResponse<MentorApplicationDto>>> GetMentorApplicationsAsync(int pageNumber = 1, int pageSize = 10, MentorApprovalStatus? status = null);
        Task<ServiceResult<MentorApplicationDto>> GetMentorApplicationDetailAsync(long userId);
        Task<ServiceResult> ApproveMentorApplicationAsync(long userId, long adminId);
        Task<ServiceResult> RejectMentorApplicationAsync(long userId, long adminId, string reason);
        Task<ServiceResult<MentorApplicationDto>> ReEvaluateMentorApplicationAsync(long userId, long adminId);

        // Lịch sử xẫt duyệt
        Task<ServiceResult<PagedResponse<MentorApplicationHistoryDto>>> GetApplicationHistoryAsync(ApplicationHistoryQueryParams queryParams);
        Task<ServiceResult<List<MentorApplicationHistoryDto>>> GetUserApplicationHistoryAsync(long userId);
        Task<ServiceResult<List<MentorApplicationHistoryExportDto>>> ExportApplicationHistoryForMLAsync(DateTime? fromDate = null, DateTime? toDate = null, ApplicationAction? action = null);
    }
}

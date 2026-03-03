using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminReportService
    {
        // Báo cáo
        Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(int pageNumber, int pageSize, string? status = null);
        Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId);
        Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request);

        // Bài đăng cộng đồng
        Task<ServiceResult<PagedResponse<AdminPostListDto>>> GetAllPostsAsync(AdminSearchPostDto request);
        Task<ServiceResult> AdminDeletePostAsync(long postId, string reason);
        Task<ServiceResult> AdminHidePostAsync(long postId, string reason);
        Task<ServiceResult> AdminUnhidePostAsync(long postId);

        // Bình luận cộng đồng
        Task<ServiceResult<PagedResponse<AdminCommentListDto>>> GetAllCommentsAsync(AdminSearchCommentDto request);
        Task<ServiceResult> AdminDeleteCommentAsync(long commentId, string reason);
    }
}

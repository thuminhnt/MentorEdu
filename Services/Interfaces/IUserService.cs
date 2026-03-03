using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IUserService
    {
        // Lấy hồ sơ
        Task<ServiceResult<UserDetailDto>> GetUserByIdAsync(long userId);
        Task<ServiceResult<UserDetailDto>> GetCurrentUserProfileAsync(long currentUserId);
        
        // Cập nhật hồ sơ
        Task<ServiceResult<UserDetailDto>> UpdateUserProfileAsync(long userId, UpdateUserProfileRequest request);
        Task<ServiceResult<UserDetailDto>> UpdateMentorProfileAsync(long userId, UpdateMentorProfileRequest request);
        
        // Ảnh đại diện
        Task<ServiceResult<string>> UpdateAvatarAsync(long userId, string avatarUrl);
        
        // Đặc thù học viên
        Task<ServiceResult<PagedResponse<UserBasicDto>>> GetStudentsAsync(int pageNumber, int pageSize);
        
        // Đăng ký mentor (từ trang hồ sơ)
        Task<ServiceResult> ApplyAsMentorAsync(long userId, ApplyAsMentorRequest request);
        Task<ServiceResult<MentorApplicationStatusDto>> GetMentorApplicationStatusAsync(long userId);
        
        // Quản lý tài khoản
        Task<ServiceResult> DeactivateAccountAsync(long userId);
        Task<ServiceResult> DeleteAccountAsync(long userId);
        
        // Báo cáo
        Task<ServiceResult<ReportDto>> CreateReportAsync(long reporterId, CreateReportDto request);
    }
}

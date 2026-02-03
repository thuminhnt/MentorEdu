using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IUserService
    {
        // Get Profile
        Task<ServiceResult<UserDetailDto>> GetUserByIdAsync(long userId);
        Task<ServiceResult<UserDetailDto>> GetCurrentUserProfileAsync(long currentUserId);
        
        // Update Profile
        Task<ServiceResult<UserDetailDto>> UpdateUserProfileAsync(long userId, UpdateUserProfileRequest request);
        Task<ServiceResult<UserDetailDto>> UpdateMentorProfileAsync(long userId, UpdateMentorProfileRequest request);
        
        // Avatar
        Task<ServiceResult<string>> UpdateAvatarAsync(long userId, string avatarUrl);
        
        // Student specific
        Task<ServiceResult<PagedResponse<UserBasicDto>>> GetStudentsAsync(int pageNumber, int pageSize);
        
        // Mentor Application (from Profile page)
        Task<ServiceResult> ApplyAsMentorAsync(long userId, ApplyAsMentorRequest request);
        Task<ServiceResult<MentorApplicationStatusDto>> GetMentorApplicationStatusAsync(long userId);
        
        // Account management
        Task<ServiceResult> DeactivateAccountAsync(long userId);
        Task<ServiceResult> DeleteAccountAsync(long userId);
        
        // Report
        Task<ServiceResult<ReportDto>> CreateReportAsync(long reporterId, CreateReportDto request);
    }
}

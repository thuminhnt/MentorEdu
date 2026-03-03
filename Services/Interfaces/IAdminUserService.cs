using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.User;
using MentorEdu.DTOs.Admin;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null);
        Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(long userId);
        Task<ServiceResult<UserDetailDto>> AdminCreateUserAsync(AdminCreateUserDto request);
        Task<ServiceResult<UserDetailDto>> AdminUpdateUserAsync(long userId, AdminUpdateUserDto request);
        Task<ServiceResult> AdminDeleteUserAsync(long userId);
        Task<ServiceResult> ActivateUserAsync(long userId);
        Task<ServiceResult> DeactivateUserAsync(long userId);
        Task<ServiceResult> BanUserAsync(long userId, string reason);
        Task<ServiceResult> VerifyUserAsync(long userId);
    }
}

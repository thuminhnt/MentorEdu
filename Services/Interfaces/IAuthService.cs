using MentorEdu.DTOs.Auth;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResult<LoginResponse>> RegisterAsync(RegisterRequest request);
        Task<ServiceResult> LogoutAsync(long userId);
        Task<ServiceResult> ChangePasswordAsync(long userId, ChangePasswordRequest request);
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ServiceResult<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ServiceResult> VerifyEmailAsync(string token, string email);
        Task<ServiceResult> ResendVerificationEmailAsync(string email);
        
        // Google OAuth
        Task<ServiceResult<LoginResponse>> GoogleLoginAsync(string email, string name, string? avatarUrl, UserRole? role = null);
    }
}

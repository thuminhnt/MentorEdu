using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MentorEdu.DTOs.Auth;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace MentorEdu.Services.Implementations
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IVerificationTokenRepository _tokenRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork, 
            IConfiguration configuration,
            IEmailService emailService,
            IVerificationTokenRepository tokenRepository,
            ILogger<AuthService> logger) 
            : base(unitOfWork)
        {
            _configuration = configuration;
            _emailService = emailService;
            _tokenRepository = tokenRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            
            if (user == null)
                return ServiceResult<LoginResponse>.FailResult("Invalid email or password", 401);

            if (!PasswordHelper.Verify(request.Password, user.PasswordHash))
                return ServiceResult<LoginResponse>.FailResult("Invalid email or password", 401);

            if (!user.IsActive)
                return ServiceResult<LoginResponse>.FailResult("Account has been locked", 403);

            if (!user.IsVerified)
                return ServiceResult<LoginResponse>.FailResult("Please verify your email before logging in", 403);

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);
            
            // Lấy trạng thái mentor nếu tồn tại (Login)
            MentorApprovalStatus? mentorStatus = null;
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(user.Id);
            if (mentorProfile != null)
            {
                mentorStatus = mentorProfile.ApprovalStatus;
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = userProfile?.FullName ?? "",
                Role = user.Role,
                AvatarUrl = userProfile?.AvatarUrl,
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiration = DateTime.UtcNow.AddHours(24),
                MentorStatus = mentorStatus
            };

            return ServiceResult<LoginResponse>.SuccessResult(response, "Login successful");
        }

        public async Task<ServiceResult<LoginResponse>> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email đã tồn tại chưa
            if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
                return ServiceResult<LoginResponse>.FailResult("Email already exists");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Tạo người dùng với vai trò Học viên
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = PasswordHelper.Hash(request.Password),
                    Role = UserRole.Student,
                    IsVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Tạo hồ sơ người dùng
                var userProfile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserProfiles.AddAsync(userProfile);

                // Tạo ví điện tử
                var wallet = new Wallet
                {
                    UserId = user.Id,
                    Balance = 0,
                    LockedBalance = 0,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Wallets.AddAsync(wallet);

                // Tạo token xác minh email
                var verificationToken = new VerificationToken
                {
                    UserId = user.Id,
                    Token = GenerateVerificationToken(),
                    Type = TokenType.EmailVerification,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _tokenRepository.AddAsync(verificationToken);
                await _unitOfWork.SaveChangesAsync();

                // Gửi email xác minh (không còn lỗi đăng ký nếu gửi email thất bại)
                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, request.FullName, verificationToken.Token);
                }
                catch (Exception)
                {
                    // Ghi log lỗi nhưng không huỷ đăng ký
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
                await _unitOfWork.SaveChangesAsync();

                var response = new LoginResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = request.FullName,
                    Role = user.Role,
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = DateTime.UtcNow.AddHours(24),
                    MentorStatus = null
                };

                return ServiceResult<LoginResponse>.CreatedResult(response, 
                    "Registration successful. Please check your email to verify your account.");
            });
        }

        public async Task<ServiceResult> LogoutAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} ({Email}) logged out at {Time}", userId, user.Email, DateTime.UtcNow);
            return ServiceResult.SuccessResult("Logout successful");
        }

        public async Task<ServiceResult> ChangePasswordAsync(long userId, ChangePasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (!PasswordHelper.Verify(request.CurrentPassword, user.PasswordHash))
                return ServiceResult.FailResult("Current password is incorrect");

            // Kiểm tra mật khẩu mới có trùng với mật khẩu hiện tại không
            if (PasswordHelper.Verify(request.NewPassword, user.PasswordHash))
                return ServiceResult.FailResult("New password cannot be the same as your current password");

            user.PasswordHash = PasswordHelper.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Password changed successfully");
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            
            if (user == null)
                return ServiceResult.SuccessResult("If the email address exists, you will receive a password reset link.");

            // Hủy bỏ các token cũ
            await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id, TokenType.PasswordReset);

            // Tạo token đặt lại mật khẩu mới
            var resetToken = new VerificationToken
            {
                UserId = user.Id,
                Token = GenerateVerificationToken(),
                Type = TokenType.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _tokenRepository.AddAsync(resetToken);
            await _unitOfWork.SaveChangesAsync();

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);
            
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, userProfile?.FullName ?? "User", resetToken.Token);
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng trả về thành công để tránh đoán email (email enumeration)
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return ServiceResult.SuccessResult("If the email exists, you will receive a password reset link");
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var tokenEntity = await _tokenRepository.GetValidTokenAsync(request.Token, request.Email, TokenType.PasswordReset);
            
            if (tokenEntity == null)
                return ServiceResult.FailResult("Invalid or expired password reset link");

            var user = tokenEntity.User;
            if (user == null)
                return ServiceResult.FailResult("User not found");

            // Kiểm tra mật khẩu mới có trùng với mật khẩu hiện tại không
            if (PasswordHelper.Verify(request.NewPassword, user.PasswordHash))
                return ServiceResult.FailResult("New password cannot be the same as your current password");

            user.PasswordHash = PasswordHelper.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            tokenEntity.IsUsed = true;

            _unitOfWork.Users.Update(user);
            _tokenRepository.Update(tokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Password reset successful");
        }

        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
                return ServiceResult<LoginResponse>.UnauthorizedResult("Invalid token");

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
                return ServiceResult<LoginResponse>.UnauthorizedResult("Invalid token");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                return ServiceResult<LoginResponse>.UnauthorizedResult("Invalid token");

            // Xác thực refresh token đang lưu trữ
            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
                return ServiceResult<LoginResponse>.UnauthorizedResult("Invalid or expired refresh token");

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Xoay vòng refresh token (cấp phát mới sau mỗi lần làm mới)
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = userProfile?.FullName ?? "",
                Role = user.Role,
                AvatarUrl = userProfile?.AvatarUrl,
                Token = newToken,
                RefreshToken = newRefreshToken,
                TokenExpiration = DateTime.UtcNow.AddHours(24)
            };

            return ServiceResult<LoginResponse>.SuccessResult(response);
        }

        public async Task<ServiceResult> VerifyEmailAsync(string token, string email)
        {
            var tokenEntity = await _tokenRepository.GetValidTokenAsync(token, email, TokenType.EmailVerification);
            
            if (tokenEntity == null)
            {
                // Token không hợp lệ hoặc hết hạn — kiểm tra xem email đã được xác minh chưa
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(email);
                if (existingUser != null && existingUser.IsVerified)
                {
                    return ServiceResult.SuccessResult("Email has already been verified");
                }
                return ServiceResult.FailResult("Invalid or expired verification link");
            }

            var user = tokenEntity.User;
            if (user == null)
                return ServiceResult.FailResult("User not found");

            if (user.IsVerified)
                return ServiceResult.SuccessResult("Email has already been verified");

            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            tokenEntity.IsUsed = true;

            _unitOfWork.Users.Update(user);
            _tokenRepository.Update(tokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Email verified successfully! You can now log in.");
        }

        public async Task<ServiceResult> ResendVerificationEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user == null)
                return ServiceResult.SuccessResult("If the email exists, you will receive a verification email");

            if (user.IsVerified)
                return ServiceResult.FailResult("Email has already been verified");

            // Hủy bỏ các token xác minh cũ
            await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id, TokenType.EmailVerification);

            // Tạo token xác minh email mới
            var verificationToken = new VerificationToken
            {
                UserId = user.Id,
                Token = GenerateVerificationToken(),
                Type = TokenType.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _tokenRepository.AddAsync(verificationToken);
            await _unitOfWork.SaveChangesAsync();

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);

            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, userProfile?.FullName ?? "User", verificationToken.Token);
            }
            catch (Exception)
            {
                // Ghi log lỗi nhưng vẫn trả về thành công (gửi lại mail)
            }

            return ServiceResult.SuccessResult("Verification email has been resent. Please check your inbox.");
        }

        #region Private Methods

        private string GenerateVerificationToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "MentorEduSecretKey123456789012345678901234567890";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MentorEdu";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "MentorEduApp";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "MentorEduSecretKey123456789012345678901234567890";

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Google OAuth

        public async Task<ServiceResult<LoginResponse>> GoogleLoginAsync(string email, string name, string? avatarUrl, UserRole? role = null, bool isRegisterRequest = false)
        {
            // Kiểm tra người dùng đã tồn tại chưa
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user != null && isRegisterRequest)
            {
                return ServiceResult<LoginResponse>.FailResult("This email is already registered. Please sign in.", 400);
            }

            if (user == null)
            {
                // Tạo người dùng mới từ tài khoản Google — luôn là Học viên
                await _unitOfWork.ExecuteWithStrategyAsync(async () =>
                {
                    user = new User
                    {
                        Email = email,
                        PasswordHash = PasswordHelper.Hash(Guid.NewGuid().ToString()), // Mật khẩu ngẫo đối với người dùng OAuth
                        Role = UserRole.Student, // Luôn là Học viên — có thể đăng ký làm mentor sau
                        IsVerified = true, // Tài khoản Google được xác minh trước rồi
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Users.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    // Tạo hồ sơ người dùng (Google)
                    var userProfile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = name,
                        AvatarUrl = avatarUrl,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserProfiles.AddAsync(userProfile);

                    // Tạo ví điện tử (Google)
                    var wallet = new Wallet
                    {
                        UserId = user.Id,
                        Balance = 0,
                        LockedBalance = 0,
                        Currency = "VND",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Wallets.AddAsync(wallet);
                    await _unitOfWork.SaveChangesAsync();
                });
            }
            else if (!user.IsActive)
            {
                return ServiceResult<LoginResponse>.FailResult("Account has been locked", 403);
            }

            // Lấy hồ sơ người dùng để lấy tên và ảnh đại diện
            // Tại điểm này người dùng luôn tồn tại (tìm thấy hoặc vừa tạo mới)
            var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user!.Id);
            
            // Lấy trạng thái mentor nếu tồn tại (Google Login)
            MentorApprovalStatus? mentorStatus = null;
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(user.Id);
            if (mentorProfile != null)
            {
                mentorStatus = mentorProfile.ApprovalStatus;
            }

            // Tạo JWT và refresh token mới
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = profile?.FullName ?? name ?? "User",
                Role = user.Role,
                AvatarUrl = profile?.AvatarUrl ?? avatarUrl,
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiration = DateTime.UtcNow.AddHours(24),
                MentorStatus = mentorStatus
            };

            return ServiceResult<LoginResponse>.SuccessResult(response, "Google login successful");
        }

        #endregion
    }
}

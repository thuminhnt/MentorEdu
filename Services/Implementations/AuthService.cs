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

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration) 
            : base(unitOfWork)
        {
            _configuration = configuration;
        }

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            
            if (user == null)
                return ServiceResult<LoginResponse>.FailResult("Invalid email or password", 401);

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return ServiceResult<LoginResponse>.FailResult("Invalid email or password", 401);

            if (!user.IsActive)
                return ServiceResult<LoginResponse>.FailResult("Account has been locked", 403);

            if (!user.IsVerified)
                return ServiceResult<LoginResponse>.FailResult("Please verify your email before logging in", 403);

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = userProfile?.FullName ?? "",
                Role = user.Role,
                AvatarUrl = userProfile?.AvatarUrl,
                Token = token,
                RefreshToken = refreshToken,
                TokenExpiration = DateTime.UtcNow.AddHours(24)
            };

            return ServiceResult<LoginResponse>.SuccessResult(response, "Login successful");
        }

        public async Task<ServiceResult<LoginResponse>> RegisterAsync(RegisterRequest request)
        {
            // Check email exists
            if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
                return ServiceResult<LoginResponse>.FailResult("Email already exists");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Create user
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    Role = request.Role,
                    IsVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Create user profile
                var userProfile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserProfiles.AddAsync(userProfile);

                // Create mentor profile if role is Mentor
                if (request.Role == UserRole.Mentor)
                {
                    var mentorProfile = new MentorProfile
                    {
                        UserId = user.Id,
                        RatingAvg = 0,
                        RatingCount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.MentorProfiles.AddAsync(mentorProfile);
                }

                // Create wallet
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
                await _unitOfWork.CommitTransactionAsync();

                // TODO: Send verification email

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                var response = new LoginResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = request.FullName,
                    Role = user.Role,
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = DateTime.UtcNow.AddHours(24)
                };

                return ServiceResult<LoginResponse>.CreatedResult(response, "Registration successful. Please check your email to verify your account.");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult> LogoutAsync(long userId)
        {
            // TODO: Invalidate refresh token in database
            await Task.CompletedTask;
            return ServiceResult.SuccessResult("Logout successful");
        }

        public async Task<ServiceResult> ChangePasswordAsync(long userId, ChangePasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return ServiceResult.FailResult("Current password is incorrect");

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Password changed successfully");
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            
            if (user == null)
                return ServiceResult.SuccessResult("If the email exists, you will receive a password reset link");

            // TODO: Generate reset token and send email

            return ServiceResult.SuccessResult("If the email exists, you will receive a password reset link");
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // TODO: Validate reset token
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            
            if (user == null)
                return ServiceResult.FailResult("Invalid or expired token");

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
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

            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(user.Id);

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

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
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user == null)
                return ServiceResult.FailResult("Invalid token");

            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Email verified successfully");
        }

        public async Task<ServiceResult> ResendVerificationEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user == null)
                return ServiceResult.SuccessResult("If the email exists, you will receive a verification email");

            if (user.IsVerified)
                return ServiceResult.FailResult("Email has already been verified");

            // TODO: Generate new verification token and send email

            return ServiceResult.SuccessResult("Verification email has been resent");
        }

        #region Private Methods

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

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        #endregion
    }
}

using MentorEdu.DTOs.Auth;
using MentorEdu.DTOs.Common;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly string _frontendUrl;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
            _frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<LoginResponse>.FailResponse("Invalid request data"));

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<LoginResponse>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<LoginResponse>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Đăng nhập tài khoản
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<LoginResponse>.FailResponse("Invalid request data"));

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<LoginResponse>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Đăng xuất tài khoản
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            var userId = GetCurrentUserId();
            var result = await _authService.LogoutAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Làm mới access token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<LoginResponse>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _authService.ChangePasswordAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Yêu cầu đặt lại mật khẩu
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Đặt lại mật khẩu bằng token
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var result = await _authService.ResetPasswordAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Xác minh email
        /// </summary>
        [HttpGet("verify-email")]
        public async Task<ActionResult<ApiResponse>> VerifyEmail([FromQuery] string token, [FromQuery] string email)
        {
            var result = await _authService.VerifyEmailAsync(token, email);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Gửi lại email xác minh
        /// </summary>
        [HttpPost("resend-verification")]
        public async Task<ActionResult<ApiResponse>> ResendVerification([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ResendVerificationEmailAsync(request.Email);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #region Google OAuth

        /// <summary>
        /// Khởi tạo đăng nhập qua Google OAuth - chuyển hướng đến Google
        /// </summary>
        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null, [FromQuery] int? role = null, [FromQuery] string? mode = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
                Items = { 
                    { "returnUrl", returnUrl ?? "/" },
                    { "mode", mode ?? "" }
                }
            };
            
            if (role.HasValue)
            {
                properties.Items.Add("role", role.Value.ToString());
            }

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Callback Google OAuth - xử lý phản hồi từ Google
        /// </summary>
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Check if user cancelled the login BEFORE authenticating
            var error = HttpContext.Request.Query["error"].ToString();
            if (!string.IsNullOrEmpty(error))
            {
                // User cancelled or denied access - redirect without authenticating
                var redirectPage = "login"; // Default to login page
                
                if (error == "access_denied")
                {
                    return Redirect($"{_frontendUrl}/{redirectPage}?cancelled=true");
                }
                return Redirect($"{_frontendUrl}/{redirectPage}?error={Uri.EscapeDataString(error)}");
            }

            // Try to authenticate - wrap in try-catch to handle any authentication failures
            AuthenticateResult authenticateResult2;
            try
            {
                authenticateResult2 = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception)
            {
                return Redirect($"{_frontendUrl}/login?error=Authentication%20failed");
            }

            if (!authenticateResult2.Succeeded)
            {
                // Redirect to frontend with error
                return Redirect($"{_frontendUrl}/login?error=Authentication%20failed");
            }

            var claims = authenticateResult2.Principal?.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Redirect($"{_frontendUrl}/login?error=Email%20not%20found");
            }

            // Get role from state if provided
            UserRole? role = null;
            if (authenticateResult2.Properties?.Items.TryGetValue("role", out var roleStr) == true 
                && int.TryParse(roleStr, out var roleInt))
            {
                role = (UserRole)roleInt;
            }

            // Get mode from state
            var mode = authenticateResult2.Properties?.Items.TryGetValue("mode", out var modeStr) == true ? modeStr : null;
            var isRegisterRequest = mode == "register";

            var result = await _authService.GoogleLoginAsync(email, name, avatarUrl, role, isRegisterRequest);

            if (!result.Success)
            {
                var errorMessage = Uri.EscapeDataString(result.Message ?? "Login failed");
                return Redirect($"{_frontendUrl}/login?error={errorMessage}");
            }

            // Redirect back to frontend with tokens
            return Redirect($"{_frontendUrl}/login?token={result.Data!.Token}&refreshToken={result.Data.RefreshToken}");
        }

        #endregion

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

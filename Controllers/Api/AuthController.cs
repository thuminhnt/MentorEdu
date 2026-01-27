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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new account
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
        /// Login to account
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<LoginResponse>.FailResponse("Invalid request data"));

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                    return StatusCode(result.StatusCode, ApiResponse<LoginResponse>.FailResponse(result.Message ?? ""));

                return Ok(ApiResponse<LoginResponse>.SuccessResponse(result.Data!, result.Message));

            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<LoginResponse>.FailResponse("Something went wrong. Please try again later.")
                );
            }
        }

        /// <summary>
        /// Logout from account
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
        /// Refresh access token
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
        /// Change password
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
        /// Request password reset
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
        /// Reset password with token
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
        /// Verify email
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
        /// Resend verification email
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
        /// Initiate Google OAuth login - redirects to Google
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
        /// Google OAuth callback - handles the response from Google
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
                    return Redirect($"http://localhost:5173/{redirectPage}?cancelled=true");
                }
                return Redirect($"http://localhost:5173/{redirectPage}?error={Uri.EscapeDataString(error)}");
            }

            // Try to authenticate - wrap in try-catch to handle any authentication failures
            AuthenticateResult authenticateResult2;
            try
            {
                authenticateResult2 = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception)
            {
                return Redirect("http://localhost:5173/login?error=Authentication%20failed");
            }

            if (!authenticateResult2.Succeeded)
            {
                // Redirect to frontend with error
                return Redirect("http://localhost:5173/login?error=Authentication%20failed");
            }

            var claims = authenticateResult2.Principal?.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Redirect("http://localhost:5173/login?error=Email%20not%20found");
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
                return Redirect($"http://localhost:5173/login?error={errorMessage}");
            }

            // Redirect back to frontend with tokens
            return Redirect($"http://localhost:5173/login?token={result.Data!.Token}&refreshToken={result.Data.RefreshToken}");
        }

        #endregion

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

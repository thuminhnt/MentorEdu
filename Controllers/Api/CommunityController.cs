using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Community;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityService _communityService;

        public CommunityController(ICommunityService communityService)
        {
            _communityService = communityService;
        }

        #region Posts

        /// <summary>
        /// Get all posts with pagination
        /// </summary>
        [HttpGet("posts")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommunityPostListDto>>>> GetPosts([FromQuery] SearchPostDto request)
        {
            var result = await _communityService.GetPostsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<CommunityPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<CommunityPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get recent posts
        /// </summary>
        [HttpGet("posts/recent")]
        public async Task<ActionResult<ApiResponse<List<CommunityPostListDto>>>> GetRecentPosts([FromQuery] int count = 10)
        {
            var result = await _communityService.GetRecentPostsAsync(count);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get post detail by ID
        /// </summary>
        [HttpGet("posts/{postId}")]
        public async Task<ActionResult<ApiResponse<CommunityPostDetailDto>>> GetPostById(long postId)
        {
            var userId = GetCurrentUserIdOrNull();
            var result = await _communityService.GetPostByIdAsync(postId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CommunityPostDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CommunityPostDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get current user's posts
        /// </summary>
        [HttpGet("posts/my-posts")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<CommunityPostListDto>>>> GetMyPosts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.GetMyPostsAsync(userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a new post
        /// </summary>
        [HttpPost("posts")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommunityPostDetailDto>>> CreatePost([FromBody] CreatePostDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CommunityPostDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _communityService.CreatePostAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CommunityPostDetailDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<CommunityPostDetailDto>.SuccessResponse(result.Data!, "Post created successfully"));
        }

        /// <summary>
        /// Update a post
        /// </summary>
        [HttpPut("posts/{postId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommunityPostDetailDto>>> UpdatePost(long postId, [FromBody] UpdatePostDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CommunityPostDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _communityService.UpdatePostAsync(postId, userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CommunityPostDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CommunityPostDetailDto>.SuccessResponse(result.Data!, "Post updated successfully"));
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpDelete("posts/{postId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> DeletePost(long postId)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.DeletePostAsync(postId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Comments

        /// <summary>
        /// Get comments for a post
        /// </summary>
        [HttpGet("posts/{postId}/comments")]
        public async Task<ActionResult<ApiResponse<List<CommunityCommentDto>>>> GetComments(long postId)
        {
            var result = await _communityService.GetCommentsByPostAsync(postId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityCommentDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityCommentDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a comment on a post
        /// </summary>
        [HttpPost("posts/{postId}/comments")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommunityCommentDto>>> CreateComment(long postId, [FromBody] CreateCommentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CommunityCommentDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _communityService.CreateCommentAsync(userId, new CreateCommentDto
            {
                PostId = postId,
                Content = request.Content
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CommunityCommentDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<CommunityCommentDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("comments/{commentId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> DeleteComment(long commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.DeleteCommentAsync(commentId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private long? GetCurrentUserIdOrNull()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}

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
            var userId = GetCurrentUserIdOrNull();
            var result = await _communityService.GetPostsAsync(request, userId);

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
            var userId = GetCurrentUserIdOrNull();
            var result = await _communityService.GetRecentPostsAsync(count, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get trending hashtags with post counts
        /// </summary>
        [HttpGet("trending-hashtags")]
        public async Task<ActionResult<ApiResponse<List<TrendingHashtagDto>>>> GetTrendingHashtags([FromQuery] int count = 10)
        {
            var result = await _communityService.GetTrendingHashtagsAsync(count);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<TrendingHashtagDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<TrendingHashtagDto>>.SuccessResponse(result.Data!));
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
                Content = request.Content,
                ParentCommentId = request.ParentCommentId
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

        #region Proposals

        /// <summary>
        /// Get following users' posts
        /// </summary>
        [HttpGet("posts/following")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommunityPostListDto>>>> GetFollowingPosts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.GetFollowingPostsAsync(userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<CommunityPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<CommunityPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Send proposal to a post (Mentor only)
        /// </summary>
        [HttpPost("posts/{postId}/proposals")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CommunityProposalDto>>> SendProposal(long postId, [FromBody] SendProposalDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CommunityProposalDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _communityService.SendProposalAsync(userId, postId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CommunityProposalDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<CommunityProposalDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Get proposals for a post
        /// </summary>
        [HttpGet("posts/{postId}/proposals")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<CommunityProposalDto>>>> GetProposalsByPost(long postId)
        {
            var result = await _communityService.GetProposalsByPostAsync(postId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityProposalDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityProposalDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Accept a proposal (Post author only)
        /// </summary>
        [HttpPut("proposals/{proposalId}/accept")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AcceptProposalResultDto>>> AcceptProposal(long proposalId)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.AcceptProposalAsync(userId, proposalId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AcceptProposalResultDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AcceptProposalResultDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Reject a proposal (Post author only)
        /// </summary>
        [HttpPut("proposals/{proposalId}/reject")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RejectProposal(long proposalId)
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.RejectProposalAsync(userId, proposalId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Get my sent proposals (Mentor)
        /// </summary>
        [HttpGet("proposals/my-proposals")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<CommunityProposalDto>>>> GetMyProposals()
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.GetMyProposalsAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityProposalDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityProposalDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get proposals received for my posts (Student)
        /// </summary>
        [HttpGet("proposals/received")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<CommunityProposalDto>>>> GetReceivedProposals()
        {
            var userId = GetCurrentUserId();
            var result = await _communityService.GetReceivedProposalsAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CommunityProposalDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CommunityProposalDto>>.SuccessResponse(result.Data!));
        }

        #endregion

        #region Follow

        /// <summary>
        /// Follow a user
        /// </summary>
        [HttpPost("follow/{userId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> FollowUser(long userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _communityService.FollowUserAsync(currentUserId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Unfollow a user
        /// </summary>
        [HttpDelete("follow/{userId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UnfollowUser(long userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _communityService.UnfollowUserAsync(currentUserId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Check if following a user
        /// </summary>
        [HttpGet("follow/{userId}/status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> IsFollowing(long userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _communityService.IsFollowingAsync(currentUserId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<bool>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<bool>.SuccessResponse(result.Data));
        }

        /// <summary>
        /// Get followers count and list for a user
        /// </summary>
        [HttpGet("follow/{userId}/followers")]
        public async Task<ActionResult<ApiResponse<FollowListDto>>> GetFollowers(long userId)
        {
            var result = await _communityService.GetFollowersAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<FollowListDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<FollowListDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get following count and list for a user
        /// </summary>
        [HttpGet("follow/{userId}/following")]
        public async Task<ActionResult<ApiResponse<FollowListDto>>> GetFollowing(long userId)
        {
            var result = await _communityService.GetFollowingAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<FollowListDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<FollowListDto>.SuccessResponse(result.Data!));
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
        public long? ParentCommentId { get; set; }
    }
}

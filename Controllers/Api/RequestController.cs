using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Request;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly IRequestService _requestService;

        public RequestController(IRequestService requestService)
        {
            _requestService = requestService;
        }

        /// <summary>
        /// Get all requests with filters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<RequestListDto>>>> GetRequests([FromQuery] SearchRequestDto request)
        {
            var result = await _requestService.GetRequestsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<RequestListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<RequestListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get open requests (for mentors to browse)
        /// </summary>
        [HttpGet("open")]
        public async Task<ActionResult<ApiResponse<PagedResponse<RequestListDto>>>> GetOpenRequests([FromQuery] SearchRequestDto request)
        {
            var result = await _requestService.GetOpenRequestsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<RequestListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<RequestListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get request detail by ID
        /// </summary>
        [HttpGet("{requestId}")]
        public async Task<ActionResult<ApiResponse<RequestDetailDto>>> GetRequestById(long requestId)
        {
            var result = await _requestService.GetByIdAsync(requestId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<RequestDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get current user's requests (students)
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResponse<RequestListDto>>>> GetMyRequests(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.GetMyRequestsAsync(userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<RequestListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<RequestListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a new request (students only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RequestDetailDto>>> CreateRequest([FromBody] CreateRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<RequestDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _requestService.CreateRequestAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<RequestDetailDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<RequestDetailDto>.SuccessResponse(result.Data!, "Request created successfully"));
        }

        /// <summary>
        /// Update a request
        /// </summary>
        [HttpPut("{requestId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RequestDetailDto>>> UpdateRequest(long requestId, [FromBody] UpdateRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<RequestDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _requestService.UpdateRequestAsync(requestId, userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<RequestDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(result.Data!, "Request updated successfully"));
        }

        /// <summary>
        /// Delete a request
        /// </summary>
        [HttpDelete("{requestId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> DeleteRequest(long requestId)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.DeleteRequestAsync(requestId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Close a request
        /// </summary>
        [HttpPost("{requestId}/close")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CloseRequest(long requestId)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.CloseRequestAsync(requestId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Get requests received by mentor
        /// </summary>
        [HttpGet("received")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResponse<RequestListDto>>>> GetReceivedRequests(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.GetReceivedRequestsAsync(userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<RequestListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<RequestListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Accept a request (mentor only)
        /// </summary>
        [HttpPost("{requestId}/accept")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AcceptRequestResultDto>>> AcceptRequest(long requestId)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.AcceptRequestAsync(requestId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AcceptRequestResultDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AcceptRequestResultDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Reject a request (mentor only)
        /// </summary>
        [HttpPost("{requestId}/reject")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RejectRequest(long requestId, [FromBody] RejectRequestDto? request = null)
        {
            var userId = GetCurrentUserId();
            var result = await _requestService.RejectRequestAsync(requestId, userId, request?.Reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

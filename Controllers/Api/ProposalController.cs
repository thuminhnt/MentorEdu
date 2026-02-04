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
    [Authorize]
    public class ProposalController : ControllerBase
    {
        private readonly IProposalService _proposalService;

        public ProposalController(IProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        /// <summary>
        /// Get proposal by ID
        /// </summary>
        [HttpGet("{proposalId}")]
        public async Task<ActionResult<ApiResponse<ProposalDetailDto>>> GetProposalById(long proposalId)
        {
            var result = await _proposalService.GetByIdAsync(proposalId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ProposalDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ProposalDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get proposals for a request
        /// </summary>
        [HttpGet("request/{requestId}")]
        public async Task<ActionResult<ApiResponse<List<ProposalListDto>>>> GetProposalsByRequest(long requestId)
        {
            var result = await _proposalService.GetProposalsByRequestAsync(requestId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<ProposalListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<ProposalListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get current mentor's proposals
        /// </summary>
        [HttpGet("my-proposals")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProposalListDto>>>> GetMyProposals(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.GetMyProposalsAsync(userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<ProposalListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<ProposalListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a new proposal (mentors only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProposalDetailDto>>> CreateProposal([FromBody] CreateProposalDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ProposalDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _proposalService.CreateProposalAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ProposalDetailDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<ProposalDetailDto>.SuccessResponse(result.Data!, "Proposal created successfully"));
        }

        /// <summary>
        /// Update a proposal
        /// </summary>
        [HttpPut("{proposalId}")]
        public async Task<ActionResult<ApiResponse<ProposalDetailDto>>> UpdateProposal(long proposalId, [FromBody] UpdateProposalDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ProposalDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _proposalService.UpdateProposalAsync(proposalId, userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ProposalDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ProposalDetailDto>.SuccessResponse(result.Data!, "Proposal updated successfully"));
        }

        /// <summary>
        /// Cancel a proposal (mentor only)
        /// </summary>
        [HttpPost("{proposalId}/cancel")]
        public async Task<ActionResult<ApiResponse>> CancelProposal(long proposalId)
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.CancelProposalAsync(proposalId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Accept a proposal (student only)
        /// </summary>
        [HttpPost("{proposalId}/accept")]
        public async Task<ActionResult<ApiResponse<AcceptProposalResultDto>>> AcceptProposal(long proposalId)
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.AcceptProposalAsync(proposalId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AcceptProposalResultDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AcceptProposalResultDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Reject a proposal (student only)
        /// </summary>
        [HttpPost("{proposalId}/reject")]
        public async Task<ActionResult<ApiResponse>> RejectProposal(long proposalId, [FromBody] RejectProposalRequest? request = null)
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.RejectProposalAsync(proposalId, userId, request?.Reason);

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

    public class RejectProposalRequest
    {
        public string? Reason { get; set; }
    }
}

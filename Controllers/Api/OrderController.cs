using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Order;
using MentorEdu.DTOs.Review;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetOrderById(long orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetByIdAsync(orderId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<OrderDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<OrderDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy đơn hàng của người dùng hiện tại (với tư cách học viên)
        /// </summary>
        [HttpGet("my-orders/student")]
        public async Task<ActionResult<ApiResponse<PagedResponse<OrderListDto>>>> GetMyStudentOrders([FromQuery] SearchOrderDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetMyOrdersAsync(userId, false, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<OrderListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<OrderListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy đơn hàng của người dùng hiện tại (với tư cách mentor)
        /// </summary>
        [HttpGet("my-orders/mentor")]
        public async Task<ActionResult<ApiResponse<PagedResponse<OrderListDto>>>> GetMyMentorOrders([FromQuery] SearchOrderDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.GetMyOrdersAsync(userId, true, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<OrderListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<OrderListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        [HttpPut("{orderId}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateOrderStatus(long orderId, [FromBody] UpdateOrderStatusDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.UpdateOrderStatusAsync(orderId, userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Hoàn thành đơn hàng (chỉ học viên)
        /// </summary>
        [HttpPost("{orderId}/complete")]
        public async Task<ActionResult<ApiResponse>> CompleteOrder(long orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.CompleteOrderAsync(orderId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        public async Task<ActionResult<ApiResponse>> CancelOrder(long orderId, [FromBody] CancelOrderRequest? request = null)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.CancelOrderAsync(orderId, userId, request?.Reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Tạo khiếu nại cho đơn hàng
        /// </summary>
        [HttpPost("{orderId}/dispute")]
        public async Task<ActionResult<ApiResponse>> DisputeOrder(long orderId, [FromBody] DisputeOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _orderService.DisputeOrderAsync(orderId, userId, request.Reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #region Work Sessions

        /// <summary>
        /// Bắt đầu phiên làm việc (chỉ mentor)
        /// </summary>
        [HttpPost("{orderId}/sessions/start")]
        public async Task<ActionResult<ApiResponse<WorkSessionDetailDto>>> StartWorkSession(long orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.StartWorkSessionAsync(userId, new StartWorkSessionDto { OrderId = orderId });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WorkSessionDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WorkSessionDetailDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Kết thúc phiên làm việc (chỉ mentor)
        /// </summary>
        [HttpPost("sessions/{sessionId}/end")]
        public async Task<ActionResult<ApiResponse<WorkSessionDetailDto>>> EndWorkSession(long sessionId, [FromBody] EndSessionRequest? request = null)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.EndWorkSessionAsync(userId, new EndWorkSessionDto 
            { 
                SessionId = sessionId, 
                Pause = request?.Pause ?? false 
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WorkSessionDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WorkSessionDetailDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Tiếp tục phiên làm việc đã tạm dừng (chỉ mentor)
        /// </summary>
        [HttpPost("sessions/{sessionId}/resume")]
        public async Task<ActionResult<ApiResponse<WorkSessionDetailDto>>> ResumeWorkSession(long sessionId)
        {
            var userId = GetCurrentUserId();
            var result = await _orderService.ResumeWorkSessionAsync(userId, new ResumeWorkSessionDto { SessionId = sessionId });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WorkSessionDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WorkSessionDetailDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Lấy tóm tắt phiên làm việc của đơn hàng
        /// </summary>
        [HttpGet("{orderId}/sessions/summary")]
        public async Task<ActionResult<ApiResponse<WorkSessionSummaryDto>>> GetWorkSessionSummary(long orderId)
        {
            var result = await _orderService.GetWorkSessionSummaryAsync(orderId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WorkSessionSummaryDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WorkSessionSummaryDto>.SuccessResponse(result.Data!));
        }

        #endregion

        #region Reviews

        /// <summary>
        /// Tạo đánh giá cho đơn hàng đã hoàn thành
        /// </summary>
        [HttpPost("{orderId}/reviews")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview(long orderId, [FromBody] CreateReviewRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ReviewDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _orderService.CreateReviewAsync(userId, new CreateReviewDto
            {
                OrderId = orderId,
                Rating = request.Rating,
                Comment = request.Comment
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ReviewDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<ReviewDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Lấy đánh giá của đơn hàng
        /// </summary>
        [HttpGet("{orderId}/reviews")]
        public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetOrderReviews(long orderId)
        {
            var result = await _orderService.GetOrderReviewsAsync(orderId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<ReviewDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<ReviewDto>>.SuccessResponse(result.Data!));
        }

        #endregion

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }

    public class DisputeOrderRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class EndSessionRequest
    {
        public bool Pause { get; set; } = false;
    }

    public class CreateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}

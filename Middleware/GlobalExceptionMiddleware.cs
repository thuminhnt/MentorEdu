using System.Net;
using System.Text.Json;

namespace MentorEdu.Middleware
{
    /// <summary>
    /// Middleware này dùng để xử lý tất cả lỗi (exception) phát sinh trong hệ thống.
    /// Khi có lỗi chưa được xử lý, middleware sẽ trả về phản hồi JSON chuẩn, không để lộ thông tin nội bộ (stack trace) ra ngoài client.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chưa được xử lý
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                // Trả về phản hồi lỗi chuẩn cho client
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Xác định loại lỗi để trả về mã lỗi và thông báo phù hợp
            var (statusCode, message) = exception switch
            {
                ArgumentException or ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid request parameters."),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found."),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                message,
                statusCode = (int)statusCode
            };

            // Trả về phản hồi JSON cho client
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    /// <summary>Extension method để đăng ký middleware tiện lợi trong Program.cs</summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}

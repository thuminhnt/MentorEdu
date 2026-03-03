namespace MentorEdu.Services.Common
{
    /// <summary>
    /// Wrapper kết quả trả về từ service
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult<T> SuccessResult(T data, string? message = null)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Message = message ?? "Success",
                Data = data,
                StatusCode = 200
            };
        }

        public static ServiceResult<T> CreatedResult(T data, string? message = null)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Message = message ?? "Created successfully",
                Data = data,
                StatusCode = 201
            };
        }
        public static ServiceResult<T> FailResult(string message, int statusCode = 400)
            => new ServiceResult<T> { Success = false, Message = message, StatusCode = statusCode };

        public static ServiceResult<T> Failure(string message, int statusCode = 400)
            => new ServiceResult<T> { Success = false, Message = message, StatusCode = statusCode };

        public static ServiceResult<T> NotFoundResult(string message = "Data not found")
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };
        }

        public static ServiceResult<T> UnauthorizedResult(string message = "Unauthorized access")
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                StatusCode = 401
            };
        }

        public static ServiceResult<T> ForbiddenResult(string message = "Access denied")
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                StatusCode = 403
            };
        }

        public static ServiceResult<T> ValidationFailResult(List<string> errors)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors,
                StatusCode = 400
            };
        }

        public static ServiceResult<T> BadRequestResult(string message = "Bad request")
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                StatusCode = 400
            };
        }
    }

    /// <summary>
    /// Kết quả service không có dữ liệu
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult SuccessResult(string? message = null)
        {
            return new ServiceResult
            {
                Success = true,
                Message = message ?? "Success",
                StatusCode = 200
            };
        }

        public static ServiceResult FailResult(string message, int statusCode = 400)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ServiceResult NotFoundResult(string message = "Data not found")
        {
            return new ServiceResult
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };
        }

        public static ServiceResult UnauthorizedResult(string message = "Unauthorized access")
        {
            return new ServiceResult
            {
                Success = false,
                Message = message,
                StatusCode = 401
            };
        }

        public static ServiceResult ForbiddenResult(string message = "Access denied")
        {
            return new ServiceResult
            {
                Success = false,
                Message = message,
                StatusCode = 403
            };
        }

        public static ServiceResult BadRequestResult(string message = "Bad request")
        {
            return new ServiceResult
            {
                Success = false,
                Message = message,
                StatusCode = 400
            };
        }
    }
}

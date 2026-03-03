using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Request;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IRequestService
    {
        // CRUD
        Task<ServiceResult<RequestDetailDto>> GetByIdAsync(long requestId);
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetRequestsAsync(SearchRequestDto searchDto);
        Task<ServiceResult<RequestDetailDto>> CreateRequestAsync(long studentId, CreateRequestDto request);
        Task<ServiceResult<RequestDetailDto>> UpdateRequestAsync(long requestId, long userId, UpdateRequestDto request);
        Task<ServiceResult> DeleteRequestAsync(long requestId, long userId);
        
        // Đặc thù học viên
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetMyRequestsAsync(long studentId, int pageNumber, int pageSize);
        
        // Đặc thù mentor
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetReceivedRequestsAsync(long mentorId, int pageNumber, int pageSize);
        Task<ServiceResult<AcceptRequestResultDto>> AcceptRequestAsync(long requestId, long mentorId);
        Task<ServiceResult> RejectRequestAsync(long requestId, long mentorId, string? reason);
        
        // Quản lý trạng thái
        Task<ServiceResult> CloseRequestAsync(long requestId, long userId);
        
        // Dành cho mentor - duyệt yêu cầu đang mở
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetOpenRequestsAsync(SearchRequestDto searchDto);
    }
}

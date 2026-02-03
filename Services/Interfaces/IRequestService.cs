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
        
        // Student specific
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetMyRequestsAsync(long studentId, int pageNumber, int pageSize);
        
        // Mentor specific
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetReceivedRequestsAsync(long mentorId, int pageNumber, int pageSize);
        Task<ServiceResult<AcceptRequestResultDto>> AcceptRequestAsync(long requestId, long mentorId);
        Task<ServiceResult> RejectRequestAsync(long requestId, long mentorId, string? reason);
        
        // Status management
        Task<ServiceResult> CloseRequestAsync(long requestId, long userId);
        
        // For mentors - browse open requests
        Task<ServiceResult<PagedResponse<RequestListDto>>> GetOpenRequestsAsync(SearchRequestDto searchDto);
    }
}

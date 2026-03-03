using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Request;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IProposalService
    {
        // CRUD
        Task<ServiceResult<ProposalDetailDto>> GetByIdAsync(long proposalId);
        Task<ServiceResult<ProposalDetailDto>> CreateProposalAsync(long mentorId, CreateProposalDto request);
        Task<ServiceResult<ProposalDetailDto>> UpdateProposalAsync(long proposalId, long mentorId, UpdateProposalDto request);
        Task<ServiceResult> CancelProposalAsync(long proposalId, long mentorId);
        
        // Dành cho học viên
        Task<ServiceResult<List<ProposalListDto>>> GetProposalsByRequestAsync(long requestId);
        Task<ServiceResult<AcceptProposalResultDto>> AcceptProposalAsync(long proposalId, long studentId);
        Task<ServiceResult> RejectProposalAsync(long proposalId, long studentId, string? reason = null);
        
        // Dành cho mentor
        Task<ServiceResult<PagedResponse<ProposalListDto>>> GetMyProposalsAsync(long mentorId, int pageNumber, int pageSize);
    }
}

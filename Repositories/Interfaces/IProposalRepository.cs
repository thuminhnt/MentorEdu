using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IProposalRepository : IRepository<Proposal>
    {
        Task<Proposal?> GetByIdWithDetailsAsync(long id);
        Task<IEnumerable<Proposal>> GetByRequestIdAsync(long requestId);
        Task<IEnumerable<Proposal>> GetByPostIdAsync(long postId);
        Task<IEnumerable<Proposal>> GetByMentorIdAsync(long mentorId);
        Task<IEnumerable<Proposal>> GetByStatusAsync(ProposalStatus status);
        Task<Proposal?> GetByRequestAndMentorAsync(long requestId, long mentorId);
        Task<Proposal?> GetByPostAndMentorAsync(long postId, long mentorId);
        Task<int> GetCountByRequestIdAsync(long requestId);
        Task<int> GetCountByPostIdAsync(long postId);
        Task<IEnumerable<Proposal>> GetByPostAuthorIdAsync(long authorId);
    }
}

using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IGroupRepository : IRepository<Group>
    {
        Task<Group?> GetByInviteCodeAsync(string inviteCode);
        Task<List<Group>> GetGroupsByMentorIdAsync(long mentorId);
        Task<List<Group>> GetGroupsByUserIdAsync(long userId);
        Task<bool> IsInviteCodeUniqueAsync(string inviteCode);
    }
}

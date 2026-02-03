using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IGroupMemberRepository : IRepository<GroupMember>
    {
        Task<List<GroupMember>> GetMembersByGroupIdAsync(long groupId);
        Task<GroupMember?> GetMemberAsync(long groupId, long userId);
        Task<bool> IsMemberAsync(long groupId, long userId);
        Task DeleteAsync(GroupMember entity);
        // ho?c Task DeleteAsync(int id);
    }

}

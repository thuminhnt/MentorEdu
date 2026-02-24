using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class GroupMemberRepository : Repository<GroupMember>, IGroupMemberRepository
    {
        public GroupMemberRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<GroupMember>> GetMembersByGroupIdAsync(long groupId)
        {
            return await _context.GroupMembers
                .Include(gm => gm.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(gm => gm.GroupId == groupId)
                .OrderBy(gm => gm.JoinedAt)
                .ToListAsync();
        }

        public async Task<GroupMember?> GetMemberAsync(long groupId, long userId)
        {
            return await _context.GroupMembers
                .Include(gm => gm.User)
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        }

        public async Task<bool> IsMemberAsync(long groupId, long userId)
        {
            return await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        }
        public async Task DeleteAsync(GroupMember entity)
        {
            _context.GroupMembers.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastReadAtAsync(long groupId, long userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member != null)
            {
                member.LastReadAt = DateTime.UtcNow;
            }
        }
    }
}

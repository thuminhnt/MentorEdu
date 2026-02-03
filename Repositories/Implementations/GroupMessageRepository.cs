using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class GroupMessageRepository : Repository<GroupMessage>, IGroupMessageRepository
    {
        public GroupMessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<GroupMessage>> GetMessagesByGroupIdAsync(long groupId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.GroupMessages
                .Include(gm => gm.Sender)
                    .ThenInclude(s => s.UserProfile)
                .Where(gm => gm.GroupId == groupId)
                .OrderByDescending(gm => gm.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetMessageCountAsync(long groupId)
        {
            return await _context.GroupMessages
                .CountAsync(gm => gm.GroupId == groupId);
        }
    }
}

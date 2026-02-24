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
            // Get newest messages first (descending), then reverse for chronological display
            var messages = await _context.GroupMessages
                .Include(gm => gm.Sender)
                    .ThenInclude(s => s.UserProfile)
                .Where(gm => gm.GroupId == groupId)
                .OrderByDescending(gm => gm.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Reverse to chronological order for display
            messages.Reverse();
            return messages;
        }

        public async Task<int> GetMessageCountAsync(long groupId)
        {
            return await _context.GroupMessages
                .CountAsync(gm => gm.GroupId == groupId);
        }

        public async Task<GroupMessage?> GetByIdWithSenderAsync(long id)
        {
            return await _context.GroupMessages
                .Include(gm => gm.Sender)
                    .ThenInclude(s => s.UserProfile)
                .FirstOrDefaultAsync(gm => gm.Id == id);
        }

        public async Task<GroupMessage?> GetLatestMessageByGroupIdAsync(long groupId)
        {
            return await _context.GroupMessages
                .Include(gm => gm.Sender)
                    .ThenInclude(s => s.UserProfile)
                .Where(gm => gm.GroupId == groupId)
                .OrderByDescending(gm => gm.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetUnreadCountAsync(long groupId, long userId, DateTime? lastReadAt)
        {
            var query = _context.GroupMessages
                .Where(gm => gm.GroupId == groupId && gm.SenderId != userId);

            if (lastReadAt.HasValue)
            {
                query = query.Where(gm => gm.CreatedAt > lastReadAt.Value);
            }

            return await query.CountAsync();
        }
    }
}

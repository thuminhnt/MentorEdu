using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Message>> GetByConversationIdAsync(long conversationId)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                    .ThenInclude(s => s.UserProfile)
                .Include(m => m.MessageFiles)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Message> Items, int TotalCount)> GetPagedByConversationAsync(
            long conversationId,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.Where(m => m.ConversationId == conversationId);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(m => m.Sender)
                    .ThenInclude(s => s.UserProfile)
                .Include(m => m.MessageFiles)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.OrderBy(m => m.CreatedAt), totalCount);
        }

        public async Task<int> GetUnreadCountByUserAsync(long userId)
        {
            return await _dbSet
                .Include(m => m.Conversation)
                .Where(m => !m.IsRead && 
                       m.SenderId != userId &&
                       (m.Conversation.StudentId == userId || m.Conversation.MentorId == userId))
                .CountAsync();
        }

        public async Task MarkAsReadAsync(long conversationId, long userId)
        {
            var unreadMessages = await _dbSet
                .Where(m => m.ConversationId == conversationId && 
                       m.SenderId != userId && 
                       !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
        }
    }
}

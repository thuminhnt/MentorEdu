using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class ConversationRepository : Repository<Conversation>, IConversationRepository
    {
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Conversation?> GetByIdWithMessagesAsync(long id)
        {
            return await _dbSet
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(50))
                    .ThenInclude(m => m.Sender)
                .Include(c => c.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(c => c.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Conversation?> GetByRequestIdAsync(long requestId, long studentId, long mentorId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.RequestId == requestId && 
                                         c.StudentId == studentId && 
                                         c.MentorId == mentorId);
        }

        public async Task<Conversation?> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Include(c => c.Student)
                .Include(c => c.Mentor)
                .FirstOrDefaultAsync(c => c.OrderId == orderId);
        }

        public async Task<IEnumerable<Conversation>> GetByUserIdAsync(long userId)
        {
            return await _dbSet
                .Where(c => c.StudentId == userId || c.MentorId == userId)
                .Include(c => c.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(c => c.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.Messages.Max(m => m.CreatedAt))
                .ToListAsync();
        }

        public async Task<Conversation?> GetOrCreateConversationAsync(long? requestId, long? orderId, long studentId, long mentorId)
        {
            var conversation = await _dbSet
                .FirstOrDefaultAsync(c => 
                    (requestId.HasValue && c.RequestId == requestId) ||
                    (orderId.HasValue && c.OrderId == orderId) ||
                    (c.StudentId == studentId && c.MentorId == mentorId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    RequestId = requestId,
                    OrderId = orderId,
                    StudentId = studentId,
                    MentorId = mentorId,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbSet.AddAsync(conversation);
            }

            return conversation;
        }
    }
}

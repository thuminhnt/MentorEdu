using MentorEdu.Data;
using MentorEdu.DTOs.AIChat;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class AIChatRepository : IAIChatRepository
    {
        private readonly ApplicationDbContext _context;

        public AIChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AIChat> CreateAsync(AIChat chat)
        {
            _context.AIChats.Add(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<List<AIChatSessionDto>> GetUserSessionsAsync(long? userId, int limit = 20)
        {
            var sessions = await _context.AIChats
                .Where(c => c.UserId == userId)
                .GroupBy(c => c.SessionId)
                .Select(g => new AIChatSessionDto
                {
                    SessionId = g.Key,
                    FirstMessage = g.OrderBy(m => m.CreatedAt)
                                    .Where(m => m.Role == "user")
                                    .Select(m => m.Content)
                                    .FirstOrDefault() ?? "",
                    MessageCount = g.Count(),
                    LastMessageAt = g.Max(m => m.CreatedAt)
                })
                .OrderByDescending(s => s.LastMessageAt)
                .Take(limit)
                .ToListAsync();

            // Truncate first message for preview
            foreach (var session in sessions)
            {
                if (session.FirstMessage.Length > 100)
                {
                    session.FirstMessage = session.FirstMessage.Substring(0, 100) + "...";
                }
            }

            return sessions;
        }

        public async Task<List<AIChat>> GetSessionMessagesAsync(string sessionId, long? userId)
        {
            return await _context.AIChats
                .Where(c => c.SessionId == sessionId && c.UserId == userId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AIChat>> GetRecentMessagesAsync(string sessionId, int limit = 10)
        {
            return await _context.AIChats
                .Where(c => c.SessionId == sessionId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .OrderBy(c => c.CreatedAt) // Reorder chronologically
                .ToListAsync();
        }

        public async Task DeleteSessionAsync(string sessionId, long? userId)
        {
            var messages = await _context.AIChats
                .Where(c => c.SessionId == sessionId && c.UserId == userId)
                .ToListAsync();

            _context.AIChats.RemoveRange(messages);
            await _context.SaveChangesAsync();
        }
    }
}

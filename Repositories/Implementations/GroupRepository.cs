using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class GroupRepository : Repository<Group>, IGroupRepository
    {
        public GroupRepository(ApplicationDbContext context) : base(context)
        {
        } // ✅ ĐÓNG CONSTRUCTOR Ở ĐÂY

        public override async Task<Group?> GetByIdAsync(long id)
        {
            return await _context.Groups
                .Include(g => g.Mentor)
                    .ThenInclude(u => u.UserProfile)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                        .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Group?> GetByInviteCodeAsync(string inviteCode)
        {
            return await _context.Groups
                .Include(g => g.Mentor)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.InviteCode == inviteCode && g.IsActive);
        }

        public async Task<List<Group>> GetGroupsByMentorIdAsync(long mentorId)
        {
            return await _context.Groups
                .Include(g => g.Mentor)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Where(g => g.MentorId == mentorId && g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Group>> GetGroupsByUserIdAsync(long userId)
        {
            return await _context.Groups
                .Include(g => g.Mentor)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Where(g => g.IsActive && g.Members.Any(m => m.UserId == userId))
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsInviteCodeUniqueAsync(string inviteCode)
        {
            return !await _context.Groups.AnyAsync(g => g.InviteCode == inviteCode);
        }
    }
}

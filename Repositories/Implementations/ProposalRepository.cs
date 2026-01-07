using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class ProposalRepository : Repository<Proposal>, IProposalRepository
    {
        public ProposalRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Proposal?> GetByIdWithDetailsAsync(long id)
        {
            return await _dbSet
                .Include(p => p.Request)
                    .ThenInclude(r => r.Student)
                .Include(p => p.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .Include(p => p.Mentor)
                    .ThenInclude(m => m.MentorProfile)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Proposal>> GetByRequestIdAsync(long requestId)
        {
            return await _dbSet
                .Where(p => p.RequestId == requestId)
                .Include(p => p.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .Include(p => p.Mentor)
                    .ThenInclude(m => m.MentorProfile)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Proposal>> GetByMentorIdAsync(long mentorId)
        {
            return await _dbSet
                .Where(p => p.MentorId == mentorId)
                .Include(p => p.Request)
                    .ThenInclude(r => r.Student)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Proposal>> GetByStatusAsync(ProposalStatus status)
        {
            return await _dbSet
                .Where(p => p.Status == status)
                .Include(p => p.Request)
                .Include(p => p.Mentor)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Proposal?> GetByRequestAndMentorAsync(long requestId, long mentorId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.RequestId == requestId && p.MentorId == mentorId);
        }

        public async Task<int> GetCountByRequestIdAsync(long requestId)
        {
            return await _dbSet.CountAsync(p => p.RequestId == requestId);
        }
    }
}

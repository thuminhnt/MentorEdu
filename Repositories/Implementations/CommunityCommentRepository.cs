using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CommunityCommentRepository : Repository<CommunityComment>, ICommunityCommentRepository
    {
        public CommunityCommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CommunityComment>> GetByPostIdAsync(long postId)
        {
            return await _dbSet
                .Where(c => c.PostId == postId)
                .Include(c => c.Author)
                    .ThenInclude(a => a.UserProfile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CommunityComment>> GetByAuthorIdAsync(long authorId)
        {
            return await _dbSet
                .Where(c => c.AuthorId == authorId)
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCountByPostIdAsync(long postId)
        {
            return await _dbSet.CountAsync(c => c.PostId == postId);
        }
    }
}

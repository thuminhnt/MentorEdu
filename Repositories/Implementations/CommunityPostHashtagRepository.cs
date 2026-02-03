using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CommunityPostHashtagRepository : Repository<CommunityPostHashtag>, ICommunityPostHashtagRepository
    {
        public CommunityPostHashtagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public new IQueryable<CommunityPostHashtag> Query()
        {
            return _dbSet.Include(cph => cph.Hashtag);
        }

        public async Task<List<CommunityPostHashtag>> GetByPostIdAsync(long postId)
        {
            return await _dbSet
                .Include(cph => cph.Hashtag)
                .Where(cph => cph.PostId == postId)
                .ToListAsync();
        }

        public async Task DeleteByPostIdAsync(long postId)
        {
            var items = await _dbSet.Where(cph => cph.PostId == postId).ToListAsync();
            _dbSet.RemoveRange(items);
        }
    }
}

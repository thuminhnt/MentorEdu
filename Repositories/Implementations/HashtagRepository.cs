using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class HashtagRepository : Repository<Hashtag>, IHashtagRepository
    {
        public HashtagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Hashtag?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(h => h.Name == name);
        }

        public async Task<IEnumerable<Hashtag>> SearchByNameAsync(string keyword)
        {
            return await _dbSet
                .Where(h => h.Name.Contains(keyword))
                .Take(20)
                .ToListAsync();
        }

        public async Task<IEnumerable<Hashtag>> GetPopularHashtagsAsync(int count)
        {
            return await _dbSet
                .Include(h => h.MentorHashtags)
                .OrderByDescending(h => h.MentorHashtags.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}

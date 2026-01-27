using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CategoryHashtagRepository : Repository<CategoryHashtag>, ICategoryHashtagRepository
    {
        public CategoryHashtagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<long>> GetCategoryIdsByHashtagNamesAsync(IEnumerable<string> hashtagNames)
        {
            var normalizedNames = hashtagNames.Select(h => h.ToLower().Trim()).ToList();
            
            return await _context.CategoryHashtags
                .Include(ch => ch.Hashtag)
                .Where(ch => normalizedNames.Contains(ch.Hashtag.Name.ToLower()))
                .Select(ch => ch.CategoryId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<Hashtag>> GetHashtagsByCategoryIdAsync(long categoryId)
        {
            return await _context.CategoryHashtags
                .Include(ch => ch.Hashtag)
                .Where(ch => ch.CategoryId == categoryId)
                .Select(ch => ch.Hashtag)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoriesByHashtagIdAsync(long hashtagId)
        {
            return await _context.CategoryHashtags
                .Include(ch => ch.Category)
                .Where(ch => ch.HashtagId == hashtagId)
                .Select(ch => ch.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<CategoryHashtag>> GetAllWithDetailsAsync()
        {
            return await _context.CategoryHashtags
                .Include(ch => ch.Category)
                .Include(ch => ch.Hashtag)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(long categoryId, long hashtagId)
        {
            return await _dbSet.AnyAsync(ch => ch.CategoryId == categoryId && ch.HashtagId == hashtagId);
        }

        public async Task RemoveAsync(long categoryId, long hashtagId)
        {
            var mapping = await _dbSet.FirstOrDefaultAsync(ch => ch.CategoryId == categoryId && ch.HashtagId == hashtagId);
            if (mapping != null)
            {
                _dbSet.Remove(mapping);
            }
        }
    }
}

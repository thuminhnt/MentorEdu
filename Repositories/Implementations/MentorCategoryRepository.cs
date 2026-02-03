using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class MentorCategoryRepository : Repository<MentorCategory>, IMentorCategoryRepository
    {
        public MentorCategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetCategoriesByMentorIdAsync(long mentorId)
        {
            return await _context.MentorCategories
                .Include(mc => mc.Category)
                .Where(mc => mc.MentorId == mentorId)
                .Select(mc => mc.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<long>> GetMentorIdsByCategoryIdsAsync(IEnumerable<long> categoryIds)
        {
            return await _context.MentorCategories
                .Where(mc => categoryIds.Contains(mc.CategoryId))
                .Select(mc => mc.MentorId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(long mentorId, long categoryId)
        {
            return await _context.MentorCategories
                .AnyAsync(mc => mc.MentorId == mentorId && mc.CategoryId == categoryId);
        }

        public async Task DeleteByMentorIdAsync(long mentorId)
        {
            var items = await _context.MentorCategories
                .Where(mc => mc.MentorId == mentorId)
                .ToListAsync();
            
            _context.MentorCategories.RemoveRange(items);
        }
    }
}

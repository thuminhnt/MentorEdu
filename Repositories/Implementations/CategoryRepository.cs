using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.ParentId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetChildCategoriesAsync(long parentId)
        {
            return await _dbSet
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<Category?> GetWithChildrenAsync(long id)
        {
            return await _dbSet
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Category>> GetAllWithHierarchyAsync()
        {
            return await _dbSet
                .Include(c => c.Children)
                .Where(c => c.ParentId == null)
                .ToListAsync();
        }
    }
}

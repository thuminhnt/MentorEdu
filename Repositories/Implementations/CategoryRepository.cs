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



        public async Task<IEnumerable<Category>> GetAllWithHierarchyAsync()
        {
            // Return all categories as flat list (no hierarchy)
            return await _dbSet.ToListAsync();
        }
    }
}

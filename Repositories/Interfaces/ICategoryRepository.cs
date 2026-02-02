using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetAllWithHierarchyAsync();
    }
}

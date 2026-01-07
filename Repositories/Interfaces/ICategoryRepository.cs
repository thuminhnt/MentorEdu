using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetChildCategoriesAsync(long parentId);
        Task<Category?> GetWithChildrenAsync(long id);
        Task<IEnumerable<Category>> GetAllWithHierarchyAsync();
    }
}

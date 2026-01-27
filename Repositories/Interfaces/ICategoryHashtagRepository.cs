using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICategoryHashtagRepository : IRepository<CategoryHashtag>
    {
        /// <summary>
        /// Get all category IDs that are mapped to the given hashtag names.
        /// </summary>
        Task<IEnumerable<long>> GetCategoryIdsByHashtagNamesAsync(IEnumerable<string> hashtagNames);

        /// <summary>
        /// Get all hashtags mapped to a specific category.
        /// </summary>
        Task<IEnumerable<Hashtag>> GetHashtagsByCategoryIdAsync(long categoryId);

        /// <summary>
        /// Get all categories mapped to a specific hashtag.
        /// </summary>
        Task<IEnumerable<Category>> GetCategoriesByHashtagIdAsync(long hashtagId);

        /// <summary>
        /// Get all mappings with included navigation properties.
        /// </summary>
        Task<IEnumerable<CategoryHashtag>> GetAllWithDetailsAsync();

        /// <summary>
        /// Check if mapping exists.
        /// </summary>
        Task<bool> ExistsAsync(long categoryId, long hashtagId);

        /// <summary>
        /// Remove mapping by category and hashtag IDs.
        /// </summary>
        Task RemoveAsync(long categoryId, long hashtagId);
    }
}

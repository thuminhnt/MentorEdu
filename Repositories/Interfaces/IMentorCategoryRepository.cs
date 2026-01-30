using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMentorCategoryRepository : IRepository<MentorCategory>
    {
        /// <summary>
        /// Get all categories for a specific mentor.
        /// </summary>
        Task<IEnumerable<Category>> GetCategoriesByMentorIdAsync(long mentorId);

        /// <summary>
        /// Get all mentor IDs by category IDs.
        /// </summary>
        Task<IEnumerable<long>> GetMentorIdsByCategoryIdsAsync(IEnumerable<long> categoryIds);

        /// <summary>
        /// Check if mentor has a specific category.
        /// </summary>
        Task<bool> ExistsAsync(long mentorId, long categoryId);

        /// <summary>
        /// Delete all categories for a mentor.
        /// </summary>
        Task DeleteByMentorIdAsync(long mentorId);
    }
}

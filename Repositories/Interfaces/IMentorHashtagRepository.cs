using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMentorHashtagRepository : IRepository<MentorHashtag>
    {
        /// <summary>
        /// Get all hashtags for a specific mentor.
        /// </summary>
        Task<IEnumerable<Hashtag>> GetHashtagsByMentorIdAsync(long mentorId);

        /// <summary>
        /// Get all mentor IDs by hashtag IDs.
        /// </summary>
        Task<IEnumerable<long>> GetMentorIdsByHashtagIdsAsync(IEnumerable<long> hashtagIds);

        /// <summary>
        /// Check if mentor has a specific hashtag.
        /// </summary>
        Task<bool> ExistsAsync(long mentorId, long hashtagId);

        /// <summary>
        /// Delete all hashtags for a mentor.
        /// </summary>
        Task DeleteByMentorIdAsync(long mentorId);
    }
}

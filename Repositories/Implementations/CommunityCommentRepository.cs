using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CommunityCommentRepository : Repository<CommunityComment>, ICommunityCommentRepository
    {
        public CommunityCommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CommunityComment>> GetByPostIdAsync(long postId)
        {
            return await _dbSet
                .Where(c => c.PostId == postId)
                .Include(c => c.Author)
                    .ThenInclude(a => a.UserProfile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get root-level comments (no parent) with pagination, plus all their replies
        /// </summary>
        public async Task<(IEnumerable<CommunityComment> Comments, int TotalCount)> GetPagedByPostIdAsync(long postId, int pageNumber, int pageSize)
        {
            // Count only root-level comments for pagination
            var totalRootComments = await _dbSet.CountAsync(c => c.PostId == postId && c.ParentCommentId == null);

            // Get paged root-level comments
            var rootCommentIds = await _dbSet
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => c.Id)
                .ToListAsync();

            // Get those root comments + ALL their nested replies in one query
            var comments = await _dbSet
                .Where(c => c.PostId == postId && (rootCommentIds.Contains(c.Id) || (c.ParentCommentId != null && rootCommentIds.Contains(c.ParentCommentId.Value))))
                .Include(c => c.Author)
                    .ThenInclude(a => a.UserProfile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Also get deeper nested replies (level 2+)
            var allCommentIds = comments.Select(c => c.Id).ToList();
            var deepReplies = await _dbSet
                .Where(c => c.PostId == postId && c.ParentCommentId != null && allCommentIds.Contains(c.ParentCommentId.Value) && !allCommentIds.Contains(c.Id))
                .Include(c => c.Author)
                    .ThenInclude(a => a.UserProfile)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var allComments = comments.Concat(deepReplies).DistinctBy(c => c.Id).ToList();

            return (allComments, totalRootComments);
        }

        public async Task<IEnumerable<CommunityComment>> GetByAuthorIdAsync(long authorId)
        {
            return await _dbSet
                .Where(c => c.AuthorId == authorId)
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCountByPostIdAsync(long postId)
        {
            return await _dbSet.CountAsync(c => c.PostId == postId);
        }
    }
}

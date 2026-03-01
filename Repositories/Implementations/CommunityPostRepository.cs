using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CommunityPostRepository : Repository<CommunityPost>, ICommunityPostRepository
    {
        public CommunityPostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public new IQueryable<CommunityPost> Query()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<CommunityPost?> GetByIdWithCommentsAsync(long id)
        {
            return await _dbSet
                .Include(p => p.Author)
                    .ThenInclude(a => a.UserProfile)
                .Include(p => p.Author)
                    .ThenInclude(a => a.MentorProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                        .ThenInclude(a => a.UserProfile)
                .Include(p => p.Proposals)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<CommunityPost>> GetByAuthorIdAsync(long authorId)
        {
            return await _dbSet
                .Where(p => p.AuthorId == authorId && !p.IsHidden)
                .Include(p => p.Author)
                    .ThenInclude(a => a.UserProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                .Include(p => p.Proposals)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<CommunityPost> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null)
        {
            var query = _dbSet.Where(p => !p.IsHidden).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(p => p.Title!.Contains(searchTerm) || 
                                        p.Content!.Contains(searchTerm));

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(p => p.Author)
                    .ThenInclude(a => a.UserProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                .Include(p => p.Proposals)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<CommunityPost> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            List<long> authorIds)
        {
            var query = _dbSet.Where(p => authorIds.Contains(p.AuthorId) && !p.IsHidden);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(p => p.Title!.Contains(searchTerm) || 
                                        p.Content!.Contains(searchTerm));

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(p => p.Author)
                    .ThenInclude(a => a.UserProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                .Include(p => p.Proposals)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<CommunityPost>> GetRecentPostsAsync(int count)
        {
            return await _dbSet
                .Where(p => !p.IsHidden)
                .Include(p => p.Author)
                    .ThenInclude(a => a.UserProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                .Include(p => p.Proposals)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}

using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class MentorHashtagRepository : Repository<MentorHashtag>, IMentorHashtagRepository
    {
        public MentorHashtagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Hashtag>> GetHashtagsByMentorIdAsync(long mentorId)
        {
            return await _context.MentorHashtags
                .Include(mh => mh.Hashtag)
                .Where(mh => mh.MentorId == mentorId)
                .Select(mh => mh.Hashtag)
                .ToListAsync();
        }

        public async Task<IEnumerable<long>> GetMentorIdsByHashtagIdsAsync(IEnumerable<long> hashtagIds)
        {
            return await _context.MentorHashtags
                .Where(mh => hashtagIds.Contains(mh.HashtagId))
                .Select(mh => mh.MentorId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(long mentorId, long hashtagId)
        {
            return await _context.MentorHashtags
                .AnyAsync(mh => mh.MentorId == mentorId && mh.HashtagId == hashtagId);
        }

        public async Task DeleteByMentorIdAsync(long mentorId)
        {
            var items = await _context.MentorHashtags
                .Where(mh => mh.MentorId == mentorId)
                .ToListAsync();
            
            _context.MentorHashtags.RemoveRange(items);
        }
    }
}

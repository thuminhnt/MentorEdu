using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class CommunityPostFileRepository : Repository<CommunityPostFile>, ICommunityPostFileRepository
    {
        public CommunityPostFileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<CommunityPostFile>> GetByPostIdAsync(long postId)
        {
            return await _dbSet
                .Where(cpf => cpf.PostId == postId)
                .OrderBy(cpf => cpf.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteByPostIdAsync(long postId)
        {
            var items = await _dbSet.Where(cpf => cpf.PostId == postId).ToListAsync();
            _dbSet.RemoveRange(items);
        }
    }
}

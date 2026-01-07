using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class RequestRepository : Repository<Request>, IRequestRepository
    {
        public RequestRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Request?> GetByIdWithDetailsAsync(long id)
        {
            return await _dbSet
                .Include(r => r.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(r => r.Category)
                .Include(r => r.Proposals)
                    .ThenInclude(p => p.Mentor)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Request>> GetByStudentIdAsync(long studentId)
        {
            return await _dbSet
                .Where(r => r.StudentId == studentId)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.Student)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Request>> GetByCategoryIdAsync(long categoryId)
        {
            return await _dbSet
                .Where(r => r.CategoryId == categoryId)
                .Include(r => r.Student)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Request>> GetOpenRequestsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == RequestStatus.Open)
                .Include(r => r.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Request> Items, int TotalCount)> GetPagedWithFiltersAsync(
            int pageNumber,
            int pageSize,
            long? categoryId = null,
            RequestStatus? status = null,
            string? searchTerm = null)
        {
            var query = _dbSet.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(r => r.CategoryId == categoryId);

            if (status.HasValue)
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r => r.Title!.Contains(searchTerm) || 
                                        r.Description!.Contains(searchTerm));

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(r => r.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}

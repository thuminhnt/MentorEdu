using System.Linq.Expressions;

namespace MentorEdu.Repositories.Interfaces
{
    /// <summary>
    /// Generic Repository Interface - ??nh ngh?a các ph??ng th?c CRUD c? b?n
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        // GET
        Task<T?> GetByIdAsync(long id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        // GET with Include (Eager Loading)
        Task<T?> GetByIdWithIncludeAsync(long id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> FindWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        // GET with Pagination
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true);

        // CREATE
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        // UPDATE
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        // DELETE
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // QUERYABLE (for complex queries)
        IQueryable<T> Query();
    }
}

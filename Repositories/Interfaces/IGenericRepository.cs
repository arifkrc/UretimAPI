using System.Linq.Expressions;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Read Operations
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllActiveAsync();
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        
        // Paging
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);
        
        // Write Operations
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(int id);
        Task DeleteAsync(T entity);
        Task SoftDeleteAsync(int id); // IsActive = false
        
        // Check Operations
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
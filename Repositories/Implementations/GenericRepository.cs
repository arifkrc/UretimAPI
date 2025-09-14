using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UretimAPI.Data;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly UretimDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(UretimDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllActiveAsync()
        {
            var property = typeof(T).GetProperty("IsActive");
            if (property != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var constantValue = Expression.Constant(true);
                var equalExpression = Expression.Equal(propertyAccess, constantValue);
                var lambda = Expression.Lambda<Func<T, bool>>(equalExpression, parameter);
                
                return await _dbSet.AsNoTracking().Where(lambda).ToListAsync();
            }
            return await GetAllAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            return entry.Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            var entitiesList = entities.ToList();
            await _dbSet.AddRangeAsync(entitiesList);
            return entitiesList;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                _context.Entry(entity).State = EntityState.Modified;
            }
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public virtual async Task DeleteAsync(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public virtual async Task SoftDeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                var property = typeof(T).GetProperty("IsActive");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(entity, false);
                    _context.Entry(entity).State = EntityState.Modified;
                }
            }
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            var keyProperty = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault();
            if (keyProperty == null) return false;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, keyProperty.Name);
            var constant = Expression.Constant(id);
            var equal = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);

            return await _dbSet.AsNoTracking().AnyAsync(lambda);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.AsNoTracking().CountAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().CountAsync(predicate);
        }
    }
}
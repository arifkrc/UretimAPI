using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UretimAPI.Data;

namespace UretimAPI.Extensions
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Adds include expressions for related data
        /// </summary>
        public static IQueryable<T> IncludeMultiple<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] includes) 
            where T : class
        {
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return query;
        }

        /// <summary>
        /// Applies standard filtering for active records
        /// </summary>
        public static IQueryable<T> WhereActive<T>(this IQueryable<T> query) where T : class
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, "IsActive");
            var constant = Expression.Constant(true);
            var equality = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);
            
            return query.Where(lambda);
        }

        /// <summary>
        /// Applies efficient pagination
        /// </summary>
        public static async Task<(IEnumerable<T> Items, int TotalCount)> ToPaginatedListAsync<T>(
            this IQueryable<T> query, 
            int pageNumber, 
            int pageSize) where T : class
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking() // Performance optimization
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class ProductionTrackingFormRepository : GenericRepository<ProductionTrackingForm>, IProductionTrackingFormRepository
    {
        public ProductionTrackingFormRepository(UretimDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Returns PTFs for a product code. Note: filtering is performed on the business `Date` field
        /// (the date the production was recorded), not on `AddedDateTime` which is only for logging/audit.
        /// </summary>
        public async Task<IEnumerable<ProductionTrackingForm>> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .Where(p => p.ProductCode == productCode && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Returns PTFs in the given business date range using the `Date` property.
        /// Do NOT use `AddedDateTime` for business date filtering.
        /// </summary>
        public async Task<IEnumerable<ProductionTrackingForm>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // IMPORTANT: Filter by p.Date (business production date). AddedDateTime is stored for audit/log and must not be used for date filters.
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .Where(p => p.Date >= startDate && p.Date <= endDate && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Returns PTFs for a shift on a specific business date (compares Date.Date).
        /// </summary>
        public async Task<IEnumerable<ProductionTrackingForm>> GetByShiftAsync(string shift, DateTime date)
        {
            // Using p.Date.Date to compare business date only
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .Where(p => p.Shift == shift && p.Date.Date == date.Date && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductionTrackingForm>> GetByOperationAsync(int operationId)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .Where(p => p.OperationId == operationId && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<int> GetTotalQuantityByProductAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(p => p.ProductCode == productCode && p.IsActive);

            // Use Date for optional filtering
            if (startDate.HasValue)
                query = query.Where(p => p.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.Date <= endDate.Value);

            return await query.SumAsync(p => p.Quantity);
        }

        public override async Task<ProductionTrackingForm?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<IEnumerable<ProductionTrackingForm>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public override async Task<IEnumerable<ProductionTrackingForm>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public override async Task<(IEnumerable<ProductionTrackingForm> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<ProductionTrackingForm, bool>>? filter = null,
            Func<IQueryable<ProductionTrackingForm>, IOrderedQueryable<ProductionTrackingForm>>? orderBy = null)
        {
            IQueryable<ProductionTrackingForm> query = _dbSet
                .Include(p => p.Product)
                .Include(p => p.Operation)
                .AsNoTracking();

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

        public IQueryable<ProductionTrackingForm> Query()
        {
            return _dbSet.AsNoTracking();
        }
    }
}
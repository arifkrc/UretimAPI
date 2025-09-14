using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<ProductionTrackingForm>> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.ProductCode == productCode && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductionTrackingForm>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.Date >= startDate && p.Date <= endDate && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductionTrackingForm>> GetByShiftAsync(string shift, DateTime date)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.Shift == shift && p.Date.Date == date.Date && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductionTrackingForm>> GetByOperationAsync(string operation)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.Operation == operation && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<int> GetTotalQuantityByProductAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(p => p.ProductCode == productCode && p.IsActive);

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
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<IEnumerable<ProductionTrackingForm>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public override async Task<IEnumerable<ProductionTrackingForm>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }
    }
}
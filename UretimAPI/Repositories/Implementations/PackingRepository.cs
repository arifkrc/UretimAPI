using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class PackingRepository : GenericRepository<Packing>, IPackingRepository
    {
        public PackingRepository(UretimDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Packing>> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.ProductCode == productCode && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Packing>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.Date >= startDate && p.Date <= endDate && p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Packing>> GetByShiftAsync(string shift, DateTime date)
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.Shift == shift && p.Date.Date == date.Date && p.IsActive)
                .ToListAsync();
        }

        public async Task<int> GetTotalPackedQuantityAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(p => p.ProductCode == productCode && p.IsActive);

            if (startDate.HasValue)
                query = query.Where(p => p.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.Date <= endDate.Value);

            return await query.SumAsync(p => p.Quantity);
        }

        public override async Task<Packing?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<IEnumerable<Packing>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Packing>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(p => p.Product)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
        }
    }
}
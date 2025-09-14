using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(UretimDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetByDocumentNoAsync(string documentNo)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.DocumentNo == documentNo && o.IsActive);
        }

        public async Task<IEnumerable<Order>> GetByCustomerAsync(string customer)
        {
            return await _dbSet
                .Where(o => o.Customer == customer && o.IsActive)
                .OrderByDescending(o => o.AddedDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet
                .Where(o => o.ProductCode == productCode && o.IsActive)
                .OrderByDescending(o => o.AddedDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByWeekAsync(string orderAddedDateTime)
        {
            return await _dbSet
                .Where(o => o.OrderAddedDateTime == orderAddedDateTime && o.IsActive)
                .OrderByDescending(o => o.AddedDateTime)
                .ToListAsync();
        }

        public async Task<bool> IsDocumentNoUniqueAsync(string documentNo, int? excludeId = null)
        {
            var query = _dbSet.Where(o => o.DocumentNo == documentNo);
            
            if (excludeId.HasValue)
                query = query.Where(o => o.Id != excludeId.Value);
            
            return !await query.AnyAsync();
        }
    }
}
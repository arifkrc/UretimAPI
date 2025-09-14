using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(UretimDbContext context) : base(context)
        {
        }

        public async Task<Product?> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive);
        }

        public async Task<IEnumerable<Product>> GetByTypeAsync(string type)
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .Where(p => p.Type == type && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByLastOperationAsync(int operationId)
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .Where(p => p.LastOperationId == operationId && p.IsActive)
                .ToListAsync();
        }

        public async Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null)
        {
            var query = _dbSet.Where(p => p.ProductCode == productCode);
            
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);
            
            return !await query.AnyAsync();
        }

        public override async Task<Product?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Product>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(p => p.LastOperation)
                .Where(p => p.IsActive)
                .ToListAsync();
        }
    }
}
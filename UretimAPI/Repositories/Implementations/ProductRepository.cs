using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(UretimDbContext context) : base(context) { }

        public async Task<Product?> GetByProductCodeAsync(string productCode)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive);
        }

        public async Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null)
        {
            return !await _dbSet.AnyAsync(p => p.ProductCode == productCode && (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        public async Task<IEnumerable<Product>> GetAllActiveAsync()
        {
            return await _dbSet.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByTypeAsync(string type)
        {
            return await _dbSet.Where(p => p.Type == type && p.IsActive).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByLastOperationAsync(int operationId)
        {
            return await _dbSet.Where(p => p.LastOperationId == operationId && p.IsActive).ToListAsync();
        }
    }
}
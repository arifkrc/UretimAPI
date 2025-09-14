using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class CycleTimeRepository : GenericRepository<CycleTime>, ICycleTimeRepository
    {
        public CycleTimeRepository(UretimDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CycleTime>> GetByProductIdAsync(int productId)
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .Where(c => c.ProductId == productId && c.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<CycleTime>> GetByOperationIdAsync(int operationId)
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .Where(c => c.OperationId == operationId && c.IsActive)
                .ToListAsync();
        }

        public async Task<CycleTime?> GetByProductAndOperationAsync(int productId, int operationId)
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.OperationId == operationId && c.IsActive);
        }

        public async Task<double> GetAverageCycleTimeAsync(int productId, int operationId)
        {
            var cycleTimes = await _dbSet
                .Where(c => c.ProductId == productId && c.OperationId == operationId && c.IsActive)
                .ToListAsync();

            return cycleTimes.Any() ? cycleTimes.Average(c => c.Second) : 0;
        }

        public override async Task<CycleTime?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public override async Task<IEnumerable<CycleTime>> GetAllAsync()
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .ToListAsync();
        }

        public override async Task<IEnumerable<CycleTime>> GetAllActiveAsync()
        {
            return await _dbSet
                .Include(c => c.Product)
                .Include(c => c.Operation)
                .Where(c => c.IsActive)
                .ToListAsync();
        }
    }
}
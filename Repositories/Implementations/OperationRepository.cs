using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class OperationRepository : GenericRepository<Operation>, IOperationRepository
    {
        public OperationRepository(UretimDbContext context) : base(context)
        {
        }

        public async Task<Operation?> GetByShortCodeAsync(string shortCode)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.ShortCode == shortCode && o.IsActive);
        }

        public async Task<bool> IsShortCodeUniqueAsync(string shortCode, int? excludeId = null)
        {
            var query = _dbSet.Where(o => o.ShortCode == shortCode);
            
            if (excludeId.HasValue)
                query = query.Where(o => o.Id != excludeId.Value);
            
            return !await query.AnyAsync();
        }
    }
}
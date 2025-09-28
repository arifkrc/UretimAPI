using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Entities;
using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Implementations
{
    public class ShipmentRepository : GenericRepository<Shipment>, IShipmentRepository
    {
        public ShipmentRepository(UretimDbContext context) : base(context) { }

        public async Task<IEnumerable<Shipment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet.Where(s => s.Date >= startDate && s.Date <= endDate && s.IsActive)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }
    }
}

using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IShipmentRepository : IGenericRepository<Shipment>
    {
        Task<IEnumerable<Shipment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}

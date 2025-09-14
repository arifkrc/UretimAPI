using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetByDocumentNoAsync(string documentNo);
        Task<IEnumerable<Order>> GetByCustomerAsync(string customer);
        Task<IEnumerable<Order>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<Order>> GetByWeekAsync(string orderAddedDateTime);
        Task<bool> IsDocumentNoUniqueAsync(string documentNo, int? excludeId = null);
    }
}
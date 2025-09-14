using UretimAPI.DTOs.Order;

namespace UretimAPI.Services.Interfaces
{
    public interface IOrderService : IBaseService<OrderDto, CreateOrderDto, UpdateOrderDto>
    {
        Task<OrderDto?> GetByDocumentNoAsync(string documentNo);
        Task<IEnumerable<OrderDto>> GetByCustomerAsync(string customer);
        Task<IEnumerable<OrderDto>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<OrderDto>> GetByWeekAsync(string orderAddedDateTime);
        Task<bool> IsDocumentNoUniqueAsync(string documentNo, int? excludeId = null);
    }
}
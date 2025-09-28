using UretimAPI.DTOs.Shipment;

namespace UretimAPI.Services.Interfaces
{
    public interface IShipmentService : IBaseService<ShipmentDto, CreateShipmentDto, UpdateShipmentDto>
    {
        Task<IEnumerable<ShipmentDto>> GetInactiveAsync();
        Task<(IEnumerable<ShipmentDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<ShipmentDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}

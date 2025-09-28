using UretimAPI.DTOs.Reports;
using UretimAPI.DTOs.Shipment;

namespace UretimAPI.Services.Interfaces
{
    public interface IReportingService
    {
        Task<ProductionReportDto> GetProductionReportAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ShipmentDto>> GetShipmentsForDateAsync(DateTime date);
        Task<ShipmentTotalsDto> GetShipmentTotalsForDateAsync(DateTime date);
        Task<ProductionTotalsDto> GetProductionTotalsForDateAsync(DateTime date);
        Task<int> GetTotalProducedAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CarryoverByTypeDto>> GetCarryoverCountsForDateAsync(DateTime date);
    }
}

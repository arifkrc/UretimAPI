using UretimAPI.DTOs.ProductionTrackingForm;

namespace UretimAPI.Services.Interfaces
{
    public interface IProductionTrackingFormService : IBaseService<ProductionTrackingFormDto, CreateProductionTrackingFormDto, UpdateProductionTrackingFormDto>
    {
        Task<IEnumerable<ProductionTrackingFormDto>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<ProductionTrackingFormDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ProductionTrackingFormDto>> GetByShiftAsync(string shift, DateTime date);
        Task<IEnumerable<ProductionTrackingFormDto>> GetByOperationAsync(int operationId);
        Task<int> GetTotalQuantityByProductAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null);
    }
}
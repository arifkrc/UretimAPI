using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IProductionTrackingFormRepository : IGenericRepository<ProductionTrackingForm>
    {
        Task<IEnumerable<ProductionTrackingForm>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<ProductionTrackingForm>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ProductionTrackingForm>> GetByShiftAsync(string shift, DateTime date);
        Task<IEnumerable<ProductionTrackingForm>> GetByOperationAsync(string operation);
        Task<int> GetTotalQuantityByProductAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null);
    }
}
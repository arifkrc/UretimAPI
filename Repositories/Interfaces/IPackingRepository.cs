using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IPackingRepository : IGenericRepository<Packing>
    {
        Task<IEnumerable<Packing>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<Packing>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Packing>> GetByShiftAsync(string shift, DateTime date);
        Task<int> GetTotalPackedQuantityAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null);
    }
}
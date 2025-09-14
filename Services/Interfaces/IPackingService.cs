using UretimAPI.DTOs.Packing;

namespace UretimAPI.Services.Interfaces
{
    public interface IPackingService : IBaseService<PackingDto, CreatePackingDto, UpdatePackingDto>
    {
        Task<IEnumerable<PackingDto>> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<PackingDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<PackingDto>> GetByShiftAsync(string shift, DateTime date);
        Task<int> GetTotalPackedQuantityAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null);
    }
}
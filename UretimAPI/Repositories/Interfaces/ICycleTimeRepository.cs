using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface ICycleTimeRepository : IGenericRepository<CycleTime>
    {
        Task<IEnumerable<CycleTime>> GetByProductIdAsync(int productId);
        Task<IEnumerable<CycleTime>> GetByOperationIdAsync(int operationId);
        Task<CycleTime?> GetByProductAndOperationAsync(int productId, int operationId);
        Task<double> GetAverageCycleTimeAsync(int productId, int operationId);
        Task<bool> IsProductOperationCombinationUniqueAsync(int productId, int operationId, int? excludeId = null);
    }
}
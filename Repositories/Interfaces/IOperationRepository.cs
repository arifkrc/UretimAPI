using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IOperationRepository : IGenericRepository<Operation>
    {
        Task<Operation?> GetByShortCodeAsync(string shortCode);
        Task<bool> IsShortCodeUniqueAsync(string shortCode, int? excludeId = null);
    }
}
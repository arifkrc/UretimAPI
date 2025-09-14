using UretimAPI.DTOs.Operation;

namespace UretimAPI.Services.Interfaces
{
    public interface IOperationService : IBaseService<OperationDto, CreateOperationDto, UpdateOperationDto>
    {
        Task<OperationDto?> GetByShortCodeAsync(string shortCode);
        Task<bool> IsShortCodeUniqueAsync(string shortCode, int? excludeId = null);
    }
}
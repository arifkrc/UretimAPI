using UretimAPI.DTOs.CycleTime;

namespace UretimAPI.Services.Interfaces
{
    public interface ICycleTimeService : IBaseService<CycleTimeDto, CreateCycleTimeDto, UpdateCycleTimeDto>
    {
        Task<IEnumerable<CycleTimeDto>> GetByProductIdAsync(int productId);
        Task<IEnumerable<CycleTimeDto>> GetByOperationIdAsync(int operationId);
        Task<CycleTimeDto?> GetByProductAndOperationAsync(int productId, int operationId);
        Task<double> GetAverageCycleTimeAsync(int productId, int operationId);
    }
}
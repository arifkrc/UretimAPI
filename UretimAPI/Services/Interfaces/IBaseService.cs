namespace UretimAPI.Services.Interfaces
{
    public interface IBaseService<TDto, TCreateDto, TUpdateDto> where TDto : class where TCreateDto : class where TUpdateDto : class
    {
        Task<IEnumerable<TDto>> GetAllAsync();
        Task<IEnumerable<TDto>> GetAllActiveAsync();
        Task<TDto?> GetByIdAsync(int id);
        Task<(IEnumerable<TDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null);
        Task<TDto> CreateAsync(TCreateDto createDto);
        Task<IEnumerable<TDto>> CreateBulkAsync(IEnumerable<TCreateDto> createDtos);
        Task<TDto> UpdateAsync(int id, TUpdateDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids);
        Task<bool> ExistsAsync(int id);
        
        // Activate/Deactivate metodlar?
        Task<TDto> SetActiveStatusAsync(int id, bool isActive);
        Task<IEnumerable<TDto>> GetInactiveAsync();
        Task<(IEnumerable<TDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null);
    }
}
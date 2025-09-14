using UretimAPI.DTOs.Product;

namespace UretimAPI.Services.Interfaces
{
    public interface IProductService : IBaseService<ProductDto, CreateProductDto, UpdateProductDto>
    {
        Task<ProductDto?> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<ProductDto>> GetByTypeAsync(string type);
        Task<IEnumerable<ProductDto>> GetByLastOperationAsync(int operationId);
        Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null);
    }
}
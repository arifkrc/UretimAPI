using UretimAPI.Entities;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetByProductCodeAsync(string productCode);
        Task<IEnumerable<Product>> GetByTypeAsync(string type);
        Task<IEnumerable<Product>> GetByLastOperationAsync(int operationId);
        Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null);
    }
}
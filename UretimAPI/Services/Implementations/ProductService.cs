using AutoMapper;
using UretimAPI.DTOs.Product;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<IEnumerable<ProductDto>> GetAllActiveAsync()
        {
            var products = await _unitOfWork.Products.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }

        public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: p => 
                    (string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm) || p.ProductCode.Contains(searchTerm)) &&
                    (!isActive.HasValue || p.IsActive == isActive.Value),
                orderBy: q => q.OrderBy(p => p.Name)
            );

            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return (productDtos, totalCount);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            // Validate unique product code
            var isUnique = await _unitOfWork.Products.IsProductCodeUniqueAsync(createDto.ProductCode);
            if (!isUnique)
                throw new DuplicateException("Product", "ProductCode", createDto.ProductCode);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(createDto.LastOperationId);
            if (operation == null)
                throw new NotFoundException("Operation", createDto.LastOperationId);

            var product = _mapper.Map<Product>(createDto);
            var createdProduct = await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductDto>(createdProduct);
        }

        public async Task<IEnumerable<ProductDto>> CreateBulkAsync(IEnumerable<CreateProductDto> createDtos)
        {
            var createDtosList = createDtos.ToList();
            
            // Validate all product codes at once
            var productCodes = createDtosList.Select(x => x.ProductCode).ToList();
            var existingProducts = await _unitOfWork.Products.FindAsync(p => productCodes.Contains(p.ProductCode));
            var existingProductCodes = existingProducts.Select(p => p.ProductCode).ToList();
            
            var duplicateProductCodes = productCodes.Where(pc => existingProductCodes.Contains(pc)).ToList();
            if (duplicateProductCodes.Any())
                throw new ValidationException("Duplicate product codes found", 
                    duplicateProductCodes.Select(pc => $"Product code '{pc}' already exists").ToList());

            // Check for duplicates within the input
            var inputDuplicates = productCodes.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (inputDuplicates.Any())
                throw new ValidationException("Duplicate product codes in input", 
                    inputDuplicates.Select(pc => $"Product code '{pc}' appears multiple times in the request").ToList());

            // Validate all operations exist
            var operationIds = createDtosList.Select(x => x.LastOperationId).Distinct().ToList();
            var existingOperations = await _unitOfWork.Operations.FindAsync(o => operationIds.Contains(o.Id));
            var existingOperationIds = existingOperations.Select(o => o.Id).ToList();
            var missingOperationIds = operationIds.Except(existingOperationIds).ToList();
            
            if (missingOperationIds.Any())
                throw new ValidationException("Operations not found", 
                    missingOperationIds.Select(id => $"Operation with ID {id} not found").ToList());

            var products = _mapper.Map<List<Product>>(createDtosList);
            var createdProducts = await _unitOfWork.Products.AddRangeAsync(products);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(createdProducts);
        }

        public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto updateDto)
        {
            var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
            if (existingProduct == null)
                throw new NotFoundException("Product", id);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(updateDto.LastOperationId);
            if (operation == null)
                throw new NotFoundException("Operation", updateDto.LastOperationId);

            _mapper.Map(updateDto, existingProduct);
            await _unitOfWork.Products.UpdateAsync(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductDto>(existingProduct);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.Products.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Product", id);

            await _unitOfWork.Products.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.Products.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Product", id);

            await _unitOfWork.Products.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            // Validate all IDs exist
            var existingProducts = await _unitOfWork.Products.FindAsync(p => idsList.Contains(p.Id));
            var existingIds = existingProducts.Select(p => p.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();
            
            if (nonExistingIds.Any())
                throw new ValidationException("Products not found", 
                    nonExistingIds.Select(id => $"Product with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.Products.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.Products.ExistsAsync(id);
        }

        public async Task<ProductDto?> GetByProductCodeAsync(string productCode)
        {
            var product = await _unitOfWork.Products.GetByProductCodeAsync(productCode);
            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }

        public async Task<IEnumerable<ProductDto>> GetByTypeAsync(string type)
        {
            var products = await _unitOfWork.Products.GetByTypeAsync(type);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<IEnumerable<ProductDto>> GetByLastOperationAsync(int operationId)
        {
            var products = await _unitOfWork.Products.GetByLastOperationAsync(operationId);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null)
        {
            return await _unitOfWork.Products.IsProductCodeUniqueAsync(productCode, excludeId);
        }

        public async Task<ProductDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
            if (existingProduct == null)
                throw new NotFoundException("Product", id);

            existingProduct.IsActive = isActive;
            await _unitOfWork.Products.UpdateAsync(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductDto>(existingProduct);
        }

        public async Task<IEnumerable<ProductDto>> GetInactiveAsync()
        {
            var products = await _unitOfWork.Products.FindAsync(p => !p.IsActive);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
    }
}
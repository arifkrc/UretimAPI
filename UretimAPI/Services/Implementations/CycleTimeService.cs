using AutoMapper;
using UretimAPI.DTOs.CycleTime;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class CycleTimeService : ICycleTimeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CycleTimeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CycleTimeDto>> GetAllAsync()
        {
            var cycleTimes = await _unitOfWork.CycleTimes.GetAllAsync();
            return _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
        }

        public async Task<IEnumerable<CycleTimeDto>> GetAllActiveAsync()
        {
            var cycleTimes = await _unitOfWork.CycleTimes.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
        }

        public async Task<CycleTimeDto?> GetByIdAsync(int id)
        {
            var cycleTime = await _unitOfWork.CycleTimes.GetByIdAsync(id);
            return cycleTime != null ? _mapper.Map<CycleTimeDto>(cycleTime) : null;
        }

        public async Task<(IEnumerable<CycleTimeDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<(IEnumerable<CycleTimeDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (cycleTimes, totalCount) = await _unitOfWork.CycleTimes.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: c => 
                    (string.IsNullOrEmpty(searchTerm) || c.Product.Name.Contains(searchTerm) || c.Operation.Name.Contains(searchTerm)) &&
                    (!isActive.HasValue || c.IsActive == isActive.Value),
                orderBy: q => q.OrderBy(c => c.Product.Name).ThenBy(c => c.Operation.Name)
            );

            var cycleTimeDtos = _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
            return (cycleTimeDtos, totalCount);
        }

        public async Task<CycleTimeDto> CreateAsync(CreateCycleTimeDto createDto)
        {
            // Validate product exists
            var product = await _unitOfWork.Products.GetByIdAsync(createDto.ProductId);
            if (product == null)
                throw new NotFoundException("Product", createDto.ProductId);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(createDto.OperationId);
            if (operation == null)
                throw new NotFoundException("Operation", createDto.OperationId);

            // Validate unique product-operation combination
            var isUnique = await _unitOfWork.CycleTimes.IsProductOperationCombinationUniqueAsync(createDto.ProductId, createDto.OperationId);
            if (!isUnique)
                throw new DuplicateException("CycleTime", "ProductId-OperationId", $"{createDto.ProductId}-{createDto.OperationId}");

            var cycleTime = _mapper.Map<CycleTime>(createDto);
            var createdCycleTime = await _unitOfWork.CycleTimes.AddAsync(cycleTime);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CycleTimeDto>(createdCycleTime);
        }

        public async Task<IEnumerable<CycleTimeDto>> CreateBulkAsync(IEnumerable<CreateCycleTimeDto> createDtos)
        {
            var createDtosList = createDtos.ToList();
            
            // Validate all product IDs at once
            var productIds = createDtosList.Select(x => x.ProductId).Distinct().ToList();
            var existingProducts = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));
            var existingProductIds = existingProducts.Select(p => p.Id).ToList();
            var missingProductIds = productIds.Except(existingProductIds).ToList();
            
            if (missingProductIds.Any())
                throw new ValidationException("Products not found", 
                    missingProductIds.Select(id => $"Product with ID {id} not found").ToList());

            // Validate all operation IDs at once
            var operationIds = createDtosList.Select(x => x.OperationId).Distinct().ToList();
            var existingOperations = await _unitOfWork.Operations.FindAsync(o => operationIds.Contains(o.Id));
            var existingOperationIds = existingOperations.Select(o => o.Id).ToList();
            var missingOperationIds = operationIds.Except(existingOperationIds).ToList();
            
            if (missingOperationIds.Any())
                throw new ValidationException("Operations not found", 
                    missingOperationIds.Select(id => $"Operation with ID {id} not found").ToList());

            // Check for duplicate product-operation combinations in input
            var productOperationCombinations = createDtosList.Select(x => new { x.ProductId, x.OperationId }).ToList();
            var inputDuplicates = productOperationCombinations
                .GroupBy(x => new { x.ProductId, x.OperationId })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (inputDuplicates.Any())
                throw new ValidationException("Duplicate product-operation combinations in input", 
                    inputDuplicates.Select(d => $"Product {d.ProductId} - Operation {d.OperationId} appears multiple times in the request").ToList());

            // Check for existing product-operation combinations in database
            var existingCombinations = new List<string>();
            foreach (var dto in createDtosList)
            {
                var isUnique = await _unitOfWork.CycleTimes.IsProductOperationCombinationUniqueAsync(dto.ProductId, dto.OperationId);
                if (!isUnique)
                {
                    existingCombinations.Add($"Product {dto.ProductId} - Operation {dto.OperationId} already has a cycle time");
                }
            }
            
            if (existingCombinations.Any())
                throw new ValidationException("Duplicate product-operation combinations found", existingCombinations);

            var cycleTimes = _mapper.Map<List<CycleTime>>(createDtosList);
            var createdCycleTimes = await _unitOfWork.CycleTimes.AddRangeAsync(cycleTimes);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<CycleTimeDto>>(createdCycleTimes);
        }

        public async Task<CycleTimeDto> UpdateAsync(int id, UpdateCycleTimeDto updateDto)
        {
            var existingCycleTime = await _unitOfWork.CycleTimes.GetByIdAsync(id);
            if (existingCycleTime == null)
                throw new NotFoundException("CycleTime", id);

            // Validate product exists
            var product = await _unitOfWork.Products.GetByIdAsync(updateDto.ProductId);
            if (product == null)
                throw new NotFoundException("Product", updateDto.ProductId);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(updateDto.OperationId);
            if (operation == null)
                throw new NotFoundException("Operation", updateDto.OperationId);

            // Validate unique product-operation combination if it's being changed
            if (existingCycleTime.ProductId != updateDto.ProductId || existingCycleTime.OperationId != updateDto.OperationId)
            {
                var isUnique = await _unitOfWork.CycleTimes.IsProductOperationCombinationUniqueAsync(updateDto.ProductId, updateDto.OperationId, id);
                if (!isUnique)
                    throw new DuplicateException("CycleTime", "ProductId-OperationId", $"{updateDto.ProductId}-{updateDto.OperationId}");
            }

            _mapper.Map(updateDto, existingCycleTime);
            await _unitOfWork.CycleTimes.UpdateAsync(existingCycleTime);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CycleTimeDto>(existingCycleTime);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.CycleTimes.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("CycleTime", id);

            await _unitOfWork.CycleTimes.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.CycleTimes.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("CycleTime", id);

            await _unitOfWork.CycleTimes.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            // Validate all IDs exist
            var existingCycleTimes = await _unitOfWork.CycleTimes.FindAsync(c => idsList.Contains(c.Id));
            var existingIds = existingCycleTimes.Select(c => c.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();
            
            if (nonExistingIds.Any())
                throw new ValidationException("CycleTimes not found", 
                    nonExistingIds.Select(id => $"CycleTime with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.CycleTimes.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.CycleTimes.ExistsAsync(id);
        }

        public async Task<IEnumerable<CycleTimeDto>> GetByProductIdAsync(int productId)
        {
            var cycleTimes = await _unitOfWork.CycleTimes.GetByProductIdAsync(productId);
            return _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
        }

        public async Task<IEnumerable<CycleTimeDto>> GetByOperationIdAsync(int operationId)
        {
            var cycleTimes = await _unitOfWork.CycleTimes.GetByOperationIdAsync(operationId);
            return _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
        }

        public async Task<CycleTimeDto?> GetByProductAndOperationAsync(int productId, int operationId)
        {
            var cycleTime = await _unitOfWork.CycleTimes.GetByProductAndOperationAsync(productId, operationId);
            return cycleTime != null ? _mapper.Map<CycleTimeDto>(cycleTime) : null;
        }

        public async Task<double> GetAverageCycleTimeAsync(int productId, int operationId)
        {
            return await _unitOfWork.CycleTimes.GetAverageCycleTimeAsync(productId, operationId);
        }

        public async Task<CycleTimeDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingCycleTime = await _unitOfWork.CycleTimes.GetByIdAsync(id);
            if (existingCycleTime == null)
                throw new NotFoundException("CycleTime", id);

            existingCycleTime.IsActive = isActive;
            await _unitOfWork.CycleTimes.UpdateAsync(existingCycleTime);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CycleTimeDto>(existingCycleTime);
        }

        public async Task<IEnumerable<CycleTimeDto>> GetInactiveAsync()
        {
            var cycleTimes = await _unitOfWork.CycleTimes.FindAsync(c => !c.IsActive);
            return _mapper.Map<IEnumerable<CycleTimeDto>>(cycleTimes);
        }
    }
}
using AutoMapper;
using UretimAPI.DTOs.Operation;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class OperationService : IOperationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OperationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OperationDto>> GetAllAsync()
        {
            var operations = await _unitOfWork.Operations.GetAllAsync();
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }

        public async Task<IEnumerable<OperationDto>> GetAllActiveAsync()
        {
            var operations = await _unitOfWork.Operations.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }

        public async Task<OperationDto?> GetByIdAsync(int id)
        {
            var operation = await _unitOfWork.Operations.GetByIdAsync(id);
            return operation != null ? _mapper.Map<OperationDto>(operation) : null;
        }

        public async Task<(IEnumerable<OperationDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<(IEnumerable<OperationDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (operations, totalCount) = await _unitOfWork.Operations.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: o => 
                    (string.IsNullOrEmpty(searchTerm) || o.Name.Contains(searchTerm) || o.ShortCode.Contains(searchTerm)) &&
                    (!isActive.HasValue || o.IsActive == isActive.Value),
                orderBy: q => q.OrderBy(o => o.Name)
            );

            var operationDtos = _mapper.Map<IEnumerable<OperationDto>>(operations);
            return (operationDtos, totalCount);
        }

        public async Task<OperationDto> CreateAsync(CreateOperationDto createDto)
        {
            var isUnique = await _unitOfWork.Operations.IsShortCodeUniqueAsync(createDto.ShortCode);
            if (!isUnique)
                throw new DuplicateException("Operation", "ShortCode", createDto.ShortCode);

            var operation = _mapper.Map<Operation>(createDto);
            var createdOperation = await _unitOfWork.Operations.AddAsync(operation);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OperationDto>(createdOperation);
        }

        public async Task<IEnumerable<OperationDto>> CreateBulkAsync(IEnumerable<CreateOperationDto> createDtos)
        {
            var createDtosList = createDtos.ToList();
            
            // Validate all short codes at once
            var shortCodes = createDtosList.Select(x => x.ShortCode).ToList();
            var existingShortCodes = await _unitOfWork.Operations.FindAsync(o => shortCodes.Contains(o.ShortCode));
            var existingShortCodesList = existingShortCodes.Select(o => o.ShortCode).ToList();
            
            var duplicateShortCodes = shortCodes.Where(sc => existingShortCodesList.Contains(sc)).ToList();
            if (duplicateShortCodes.Any())
                throw new ValidationException("Duplicate short codes found", 
                    duplicateShortCodes.Select(sc => $"Short code '{sc}' already exists").ToList());

            // Check for duplicates within the input
            var inputDuplicates = shortCodes.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (inputDuplicates.Any())
                throw new ValidationException("Duplicate short codes in input", 
                    inputDuplicates.Select(sc => $"Short code '{sc}' appears multiple times in the request").ToList());

            var operations = _mapper.Map<List<Operation>>(createDtosList);
            var createdOperations = await _unitOfWork.Operations.AddRangeAsync(operations);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<OperationDto>>(createdOperations);
        }

        public async Task<OperationDto> UpdateAsync(int id, UpdateOperationDto updateDto)
        {
            var existingOperation = await _unitOfWork.Operations.GetByIdAsync(id);
            if (existingOperation == null)
                throw new NotFoundException("Operation", id);

            var isUnique = await _unitOfWork.Operations.IsShortCodeUniqueAsync(updateDto.ShortCode, id);
            if (!isUnique)
                throw new DuplicateException("Operation", "ShortCode", updateDto.ShortCode);

            _mapper.Map(updateDto, existingOperation);
            await _unitOfWork.Operations.UpdateAsync(existingOperation);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OperationDto>(existingOperation);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.Operations.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Operation", id);

            await _unitOfWork.Operations.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.Operations.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Operation", id);

            await _unitOfWork.Operations.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            // Validate all IDs exist
            var existingOperations = await _unitOfWork.Operations.FindAsync(o => idsList.Contains(o.Id));
            var existingIds = existingOperations.Select(o => o.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();
            
            if (nonExistingIds.Any())
                throw new ValidationException("Operations not found", 
                    nonExistingIds.Select(id => $"Operation with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.Operations.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.Operations.ExistsAsync(id);
        }

        public async Task<OperationDto?> GetByShortCodeAsync(string shortCode)
        {
            var operation = await _unitOfWork.Operations.GetByShortCodeAsync(shortCode);
            return operation != null ? _mapper.Map<OperationDto>(operation) : null;
        }

        public async Task<bool> IsShortCodeUniqueAsync(string shortCode, int? excludeId = null)
        {
            return await _unitOfWork.Operations.IsShortCodeUniqueAsync(shortCode, excludeId);
        }

        public async Task<OperationDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingOperation = await _unitOfWork.Operations.GetByIdAsync(id);
            if (existingOperation == null)
                throw new NotFoundException("Operation", id);

            existingOperation.IsActive = isActive;
            await _unitOfWork.Operations.UpdateAsync(existingOperation);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OperationDto>(existingOperation);
        }

        public async Task<IEnumerable<OperationDto>> GetInactiveAsync()
        {
            var operations = await _unitOfWork.Operations.FindAsync(o => !o.IsActive);
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }
    }
}
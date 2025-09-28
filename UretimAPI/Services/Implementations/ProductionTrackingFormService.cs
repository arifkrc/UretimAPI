using AutoMapper;
using UretimAPI.DTOs.ProductionTrackingForm;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;
using System.Linq.Expressions;

namespace UretimAPI.Services.Implementations
{
    public class ProductionTrackingFormService : IProductionTrackingFormService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductionTrackingFormService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetAllAsync()
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetAllActiveAsync()
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<ProductionTrackingFormDto?> GetByIdAsync(int id)
        {
            var ptf = await _unitOfWork.ProductionTrackingForms.GetByIdAsync(id);
            return ptf != null ? _mapper.Map<ProductionTrackingFormDto>(ptf) : null;
        }

        public async Task<(IEnumerable<ProductionTrackingFormDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            Expression<Func<ProductionTrackingForm, bool>>? filter = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // If searchTerm is numeric, allow searching by OperationId as exact match
                if (int.TryParse(searchTerm, out var opId))
                {
                    filter = p => (p.ProductCode.Contains(searchTerm) || p.Shift.Contains(searchTerm) || p.Operation != null && p.Operation.Name.Contains(searchTerm) || p.OperationId == opId) &&
                                  (!isActive.HasValue || p.IsActive == isActive.Value);
                }
                else
                {
                    filter = p => (p.ProductCode.Contains(searchTerm) || p.Shift.Contains(searchTerm) || (p.Operation != null && p.Operation.Name.Contains(searchTerm))) &&
                                  (!isActive.HasValue || p.IsActive == isActive.Value);
                }
            }
            else
            {
                filter = p => !isActive.HasValue || p.IsActive == isActive.Value;
            }

            var (ptfs, totalCount) = await _unitOfWork.ProductionTrackingForms.GetPagedAsync(
                pageNumber,
                pageSize,
                filter: filter,
                orderBy: q => q.OrderByDescending(p => p.Date)
            );

            var ptfDtos = _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
            return (ptfDtos, totalCount);
        }

        public async Task<(IEnumerable<ProductionTrackingFormDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true);
        }

        public async Task<ProductionTrackingFormDto> CreateAsync(CreateProductionTrackingFormDto createDto)
        {
            // Validate product exists by product code
            var product = await _unitOfWork.Products.GetByProductCodeAsync(createDto.ProductCode);
            if (product == null)
                throw new NotFoundException("Product", createDto.ProductCode);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(createDto.OperationId);
            if (operation == null)
                throw new NotFoundException("Operation", createDto.OperationId);

            var ptf = _mapper.Map<ProductionTrackingForm>(createDto);
            var createdPtf = await _unitOfWork.ProductionTrackingForms.AddAsync(ptf);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductionTrackingFormDto>(createdPtf);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> CreateBulkAsync(IEnumerable<CreateProductionTrackingFormDto> createDtos)
        {
            var createDtosList = createDtos.ToList();

            // Validate all product codes at once
            var productCodes = createDtosList.Select(x => x.ProductCode).Distinct().ToList();
            var existingProducts = await _unitOfWork.Products.FindAsync(p => productCodes.Contains(p.ProductCode));
            var existingProductCodes = existingProducts.Select(p => p.ProductCode).ToList();
            var missingProductCodes = productCodes.Except(existingProductCodes).ToList();

            if (missingProductCodes.Any())
                throw new ValidationException("Products not found", 
                    missingProductCodes.Select(code => $"Product with code '{code}' not found").ToList());

            // Validate all operations exist
            var operationIds = createDtosList.Select(x => x.OperationId).Distinct().ToList();
            var existingOperations = await _unitOfWork.Operations.FindAsync(o => operationIds.Contains(o.Id));
            var existingOperationIds = existingOperations.Select(o => o.Id).ToList();
            var missingOperationIds = operationIds.Except(existingOperationIds).ToList();
            if (missingOperationIds.Any())
                throw new ValidationException("Operations not found", missingOperationIds.Select(id => $"Operation with ID {id} not found").ToList());

            var ptfs = _mapper.Map<List<ProductionTrackingForm>>(createDtosList);
            var createdPtfs = await _unitOfWork.ProductionTrackingForms.AddRangeAsync(ptfs);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(createdPtfs);
        }

        public async Task<ProductionTrackingFormDto> UpdateAsync(int id, UpdateProductionTrackingFormDto updateDto)
        {
            var existingPtf = await _unitOfWork.ProductionTrackingForms.GetByIdAsync(id);
            if (existingPtf == null)
                throw new NotFoundException("ProductionTrackingForm", id);

            // Validate product exists by product code
            var product = await _unitOfWork.Products.GetByProductCodeAsync(updateDto.ProductCode);
            if (product == null)
                throw new NotFoundException("Product", updateDto.ProductCode);

            // Validate operation exists
            var operation = await _unitOfWork.Operations.GetByIdAsync(updateDto.OperationId);
            if (operation == null)
                throw new NotFoundException("Operation", updateDto.OperationId);

            _mapper.Map(updateDto, existingPtf);
            await _unitOfWork.ProductionTrackingForms.UpdateAsync(existingPtf);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductionTrackingFormDto>(existingPtf);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.ProductionTrackingForms.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("ProductionTrackingForm", id);

            await _unitOfWork.ProductionTrackingForms.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.ProductionTrackingForms.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("ProductionTrackingForm", id);

            await _unitOfWork.ProductionTrackingForms.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.ProductionTrackingForms.ExistsAsync(id);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetByProductCodeAsync(string productCode)
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetByProductCodeAsync(productCode);
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetByDateRangeAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetByShiftAsync(string shift, DateTime date)
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetByShiftAsync(shift, date);
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetByOperationAsync(int operationId)
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetByOperationAsync(operationId);
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<int> GetTotalQuantityByProductAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _unitOfWork.ProductionTrackingForms.GetTotalQuantityByProductAsync(productCode, startDate, endDate);
        }

        public async Task<ProductionTrackingFormDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingPtf = await _unitOfWork.ProductionTrackingForms.GetByIdAsync(id);
            if (existingPtf == null)
                throw new NotFoundException("ProductionTrackingForm", id);

            existingPtf.IsActive = isActive;
            await _unitOfWork.ProductionTrackingForms.UpdateAsync(existingPtf);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductionTrackingFormDto>(existingPtf);
        }

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetInactiveAsync()
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.FindAsync(p => !p.IsActive);
            return _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            if (idsList.Count == 0)
                throw new ValidationException("ProductionTrackingForms not found", new List<string> { "At least one ID must be provided" });

            // Validate all IDs exist
            var existingPtfs = await _unitOfWork.ProductionTrackingForms.FindAsync(p => idsList.Contains(p.Id));
            var existingIds = existingPtfs.Select(p => p.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();

            if (nonExistingIds.Any())
                throw new ValidationException("ProductionTrackingForms not found", 
                    nonExistingIds.Select(id => $"ProductionTrackingForm with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.ProductionTrackingForms.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }
    }
}
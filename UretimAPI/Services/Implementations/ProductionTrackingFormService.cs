using AutoMapper;
using UretimAPI.DTOs.ProductionTrackingForm;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

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

        public async Task<(IEnumerable<ProductionTrackingFormDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<ProductionTrackingFormDto> CreateAsync(CreateProductionTrackingFormDto createDto)
        {
            // Validate product exists by product code
            var product = await _unitOfWork.Products.GetByProductCodeAsync(createDto.ProductCode);
            if (product == null)
                throw new NotFoundException("Product", createDto.ProductCode);

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

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
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

        public async Task<IEnumerable<ProductionTrackingFormDto>> GetByOperationAsync(string operation)
        {
            var ptfs = await _unitOfWork.ProductionTrackingForms.GetByOperationAsync(operation);
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

        public async Task<(IEnumerable<ProductionTrackingFormDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (ptfs, totalCount) = await _unitOfWork.ProductionTrackingForms.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: p => 
                    (string.IsNullOrEmpty(searchTerm) || p.ProductCode.Contains(searchTerm) || p.Operation.Contains(searchTerm) || p.Shift.Contains(searchTerm)) &&
                    (!isActive.HasValue || p.IsActive == isActive.Value),
                orderBy: q => q.OrderByDescending(p => p.Date)
            );

            var ptfDtos = _mapper.Map<IEnumerable<ProductionTrackingFormDto>>(ptfs);
            return (ptfDtos, totalCount);
        }
    }
}
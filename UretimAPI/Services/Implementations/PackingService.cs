using AutoMapper;
using UretimAPI.DTOs.Packing;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class PackingService : IPackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PackingService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PackingDto>> GetAllAsync()
        {
            var packings = await _unitOfWork.Packings.GetAllAsync();
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<IEnumerable<PackingDto>> GetAllActiveAsync()
        {
            var packings = await _unitOfWork.Packings.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<PackingDto?> GetByIdAsync(int id)
        {
            var packing = await _unitOfWork.Packings.GetByIdAsync(id);
            return packing != null ? _mapper.Map<PackingDto>(packing) : null;
        }

        public async Task<(IEnumerable<PackingDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<PackingDto> CreateAsync(CreatePackingDto createDto)
        {
            // Validate product exists by product code
            var product = await _unitOfWork.Products.GetByProductCodeAsync(createDto.ProductCode);
            if (product == null)
                throw new NotFoundException("Product", createDto.ProductCode);

            var packing = _mapper.Map<Packing>(createDto);
            var createdPacking = await _unitOfWork.Packings.AddAsync(packing);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PackingDto>(createdPacking);
        }

        public async Task<IEnumerable<PackingDto>> CreateBulkAsync(IEnumerable<CreatePackingDto> createDtos)
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

            var packings = _mapper.Map<List<Packing>>(createDtosList);
            var createdPackings = await _unitOfWork.Packings.AddRangeAsync(packings);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<PackingDto>>(createdPackings);
        }

        public async Task<PackingDto> UpdateAsync(int id, UpdatePackingDto updateDto)
        {
            var existingPacking = await _unitOfWork.Packings.GetByIdAsync(id);
            if (existingPacking == null)
                throw new NotFoundException("Packing", id);

            // Validate product exists by product code
            var product = await _unitOfWork.Products.GetByProductCodeAsync(updateDto.ProductCode);
            if (product == null)
                throw new NotFoundException("Product", updateDto.ProductCode);

            _mapper.Map(updateDto, existingPacking);
            await _unitOfWork.Packings.UpdateAsync(existingPacking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PackingDto>(existingPacking);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.Packings.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Packing", id);

            await _unitOfWork.Packings.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.Packings.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Packing", id);

            await _unitOfWork.Packings.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            // Validate all IDs exist
            var existingPackings = await _unitOfWork.Packings.FindAsync(p => idsList.Contains(p.Id));
            var existingIds = existingPackings.Select(p => p.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();
            
            if (nonExistingIds.Any())
                throw new ValidationException("Packings not found", 
                    nonExistingIds.Select(id => $"Packing with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.Packings.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.Packings.ExistsAsync(id);
        }

        public async Task<IEnumerable<PackingDto>> GetByProductCodeAsync(string productCode)
        {
            var packings = await _unitOfWork.Packings.GetByProductCodeAsync(productCode);
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<IEnumerable<PackingDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var packings = await _unitOfWork.Packings.GetByDateRangeAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<IEnumerable<PackingDto>> GetByShiftAsync(string shift, DateTime date)
        {
            var packings = await _unitOfWork.Packings.GetByShiftAsync(shift, date);
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<int> GetTotalPackedQuantityAsync(string productCode, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _unitOfWork.Packings.GetTotalPackedQuantityAsync(productCode, startDate, endDate);
        }

        public async Task<PackingDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingPacking = await _unitOfWork.Packings.GetByIdAsync(id);
            if (existingPacking == null)
                throw new NotFoundException("Packing", id);

            existingPacking.IsActive = isActive;
            await _unitOfWork.Packings.UpdateAsync(existingPacking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PackingDto>(existingPacking);
        }

        public async Task<IEnumerable<PackingDto>> GetInactiveAsync()
        {
            var packings = await _unitOfWork.Packings.FindAsync(p => !p.IsActive);
            return _mapper.Map<IEnumerable<PackingDto>>(packings);
        }

        public async Task<(IEnumerable<PackingDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (packings, totalCount) = await _unitOfWork.Packings.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: p => 
                    (string.IsNullOrEmpty(searchTerm) || p.ProductCode.Contains(searchTerm) || (p.Shift != null && p.Shift.Contains(searchTerm))) &&
                    (!isActive.HasValue || p.IsActive == isActive.Value),
                orderBy: q => q.OrderByDescending(p => p.Date)
            );

            var packingDtos = _mapper.Map<IEnumerable<PackingDto>>(packings);
            return (packingDtos, totalCount);
        }
    }
}
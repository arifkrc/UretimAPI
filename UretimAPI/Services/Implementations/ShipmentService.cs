using AutoMapper;
using UretimAPI.DTOs.Shipment;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class ShipmentService : IShipmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ShipmentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ShipmentDto>> GetAllAsync()
        {
            var items = await _unitOfWork.Repository<Shipment>().GetAllAsync();
            return _mapper.Map<IEnumerable<ShipmentDto>>(items);
        }

        public async Task<IEnumerable<ShipmentDto>> GetAllActiveAsync()
        {
            var items = await _unitOfWork.Shipments.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<ShipmentDto>>(items);
        }

        public async Task<ShipmentDto?> GetByIdAsync(int id)
        {
            var entity = await _unitOfWork.Shipments.GetByIdAsync(id);
            return entity != null ? _mapper.Map<ShipmentDto>(entity) : null;
        }

        public async Task<(IEnumerable<ShipmentDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var (items, total) = await _unitOfWork.Shipments.GetPagedAsync(pageNumber, pageSize);
            return (_mapper.Map<IEnumerable<ShipmentDto>>(items), total);
        }

        // IBaseService compatibility wrappers
        public async Task<(IEnumerable<ShipmentDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            // current implementation ignores searchTerm; delegate to existing paged method
            return await GetPagedAsync(pageNumber, pageSize);
        }

        public async Task<(IEnumerable<ShipmentDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            // ignore searchTerm and isActive for now; delegate to existing paged method
            return await GetPagedAsync(pageNumber, pageSize);
        }

        public async Task<ShipmentDto> CreateAsync(CreateShipmentDto createDto)
        {
            // Ensure mutually exclusive flags
            if (createDto.Abroad && createDto.Domestic)
                throw new ValidationException("Shipment flags invalid", new List<string> { "Shipment cannot be both Abroad and Domestic" });

            var entity = _mapper.Map<Shipment>(createDto);
            var created = await _unitOfWork.Shipments.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ShipmentDto>(created);
        }

        public async Task<IEnumerable<ShipmentDto>> CreateBulkAsync(IEnumerable<CreateShipmentDto> createDtos)
        {
            var dtoList = createDtos.ToList();
            var invalids = dtoList
                .Select((d, idx) => (d, idx))
                .Where(x => x.d.Abroad && x.d.Domestic)
                .ToList();

            if (invalids.Any())
            {
                var errors = invalids.Select(x => $"Item at index {x.idx} cannot be both Abroad and Domestic").ToList();
                throw new ValidationException("Invalid shipments in bulk", errors);
            }

            var entities = _mapper.Map<List<Shipment>>(createDtos);
            var created = await _unitOfWork.Shipments.AddRangeAsync(entities);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<IEnumerable<ShipmentDto>>(created);
        }

        public async Task<ShipmentDto> UpdateAsync(int id, UpdateShipmentDto updateDto)
        {
            var existing = await _unitOfWork.Shipments.GetByIdAsync(id);
            if (existing == null) throw new NotFoundException("Shipment", id);

            // Validate mutually exclusive flags
            if (updateDto.Abroad && updateDto.Domestic)
                throw new ValidationException("Shipment flags invalid", new List<string> { "Shipment cannot be both Abroad and Domestic" });

            _mapper.Map(updateDto, existing);
            await _unitOfWork.Shipments.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ShipmentDto>(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.Shipments.ExistsAsync(id);
            if (!exists) throw new NotFoundException("Shipment", id);
            await _unitOfWork.Shipments.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.Shipments.ExistsAsync(id);
            if (!exists) throw new NotFoundException("Shipment", id);
            await _unitOfWork.Shipments.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            var existing = await _unitOfWork.Shipments.FindAsync(s => idsList.Contains(s.Id));
            var existingIds = existing.Select(s => s.Id).ToList();
            var missing = idsList.Except(existingIds).ToList();
            if (missing.Any()) throw new ValidationException("Shipments not found", missing.Select(id => $"Shipment {id} not found").ToList());
            foreach (var id in existingIds) await _unitOfWork.Shipments.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.Shipments.ExistsAsync(id);
        }

        public async Task<IEnumerable<ShipmentDto>> GetInactiveAsync()
        {
            var items = await _unitOfWork.Shipments.FindAsync(s => !s.IsActive);
            return _mapper.Map<IEnumerable<ShipmentDto>>(items);
        }

        public async Task<ShipmentDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existing = await _unitOfWork.Shipments.GetByIdAsync(id);
            if (existing == null) throw new NotFoundException("Shipment", id);
            existing.IsActive = isActive;
            await _unitOfWork.Shipments.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ShipmentDto>(existing);
        }

        public async Task<IEnumerable<ShipmentDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var items = await _unitOfWork.Shipments.GetByDateRangeAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<ShipmentDto>>(items);
        }
    }
}

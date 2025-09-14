using AutoMapper;
using UretimAPI.DTOs.Order;
using UretimAPI.Entities;
using UretimAPI.Exceptions;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetAllActiveAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            return order != null ? _mapper.Map<OrderDto>(order) : null;
        }

        public async Task<(IEnumerable<OrderDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, searchTerm, true); // Default olarak sadece aktif olanlar
        }

        public async Task<OrderDto> CreateAsync(CreateOrderDto createDto)
        {
            // Validate unique document number
            var isUnique = await _unitOfWork.Orders.IsDocumentNoUniqueAsync(createDto.DocumentNo);
            if (!isUnique)
                throw new DuplicateException("Order", "DocumentNo", createDto.DocumentNo);

            var order = _mapper.Map<Order>(createDto);
            var createdOrder = await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderDto>(createdOrder);
        }

        public async Task<IEnumerable<OrderDto>> CreateBulkAsync(IEnumerable<CreateOrderDto> createDtos)
        {
            var createDtosList = createDtos.ToList();
            
            // Validate all document numbers at once
            var documentNos = createDtosList.Select(x => x.DocumentNo).ToList();
            var existingOrders = await _unitOfWork.Orders.FindAsync(o => documentNos.Contains(o.DocumentNo));
            var existingDocumentNos = existingOrders.Select(o => o.DocumentNo).ToList();
            
            var duplicateDocumentNos = documentNos.Where(dn => existingDocumentNos.Contains(dn)).ToList();
            if (duplicateDocumentNos.Any())
                throw new ValidationException("Duplicate document numbers found", 
                    duplicateDocumentNos.Select(dn => $"Document number '{dn}' already exists").ToList());

            // Check for duplicates within the input
            var inputDuplicates = documentNos.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (inputDuplicates.Any())
                throw new ValidationException("Duplicate document numbers in input", 
                    inputDuplicates.Select(dn => $"Document number '{dn}' appears multiple times in the request").ToList());

            var orders = _mapper.Map<List<Order>>(createDtosList);
            var createdOrders = await _unitOfWork.Orders.AddRangeAsync(orders);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(createdOrders);
        }

        public async Task<OrderDto> UpdateAsync(int id, UpdateOrderDto updateDto)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(id);
            if (existingOrder == null)
                throw new NotFoundException("Order", id);

            _mapper.Map(updateDto, existingOrder);
            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderDto>(existingOrder);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exists = await _unitOfWork.Orders.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Order", id);

            await _unitOfWork.Orders.DeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var exists = await _unitOfWork.Orders.ExistsAsync(id);
            if (!exists)
                throw new NotFoundException("Order", id);

            await _unitOfWork.Orders.SoftDeleteAsync(id);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> BulkSoftDeleteAsync(IEnumerable<int> ids)
        {
            var idsList = ids.ToList();
            
            // Validate all IDs exist
            var existingOrders = await _unitOfWork.Orders.FindAsync(o => idsList.Contains(o.Id));
            var existingIds = existingOrders.Select(o => o.Id).ToList();
            var nonExistingIds = idsList.Except(existingIds).ToList();
            
            if (nonExistingIds.Any())
                throw new ValidationException("Orders not found", 
                    nonExistingIds.Select(id => $"Order with ID {id} not found").ToList());

            foreach (var id in existingIds)
            {
                await _unitOfWork.Orders.SoftDeleteAsync(id);
            }
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _unitOfWork.Orders.ExistsAsync(id);
        }

        public async Task<OrderDto?> GetByDocumentNoAsync(string documentNo)
        {
            var order = await _unitOfWork.Orders.GetByDocumentNoAsync(documentNo);
            return order != null ? _mapper.Map<OrderDto>(order) : null;
        }

        public async Task<IEnumerable<OrderDto>> GetByCustomerAsync(string customer)
        {
            var orders = await _unitOfWork.Orders.GetByCustomerAsync(customer);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetByProductCodeAsync(string productCode)
        {
            var orders = await _unitOfWork.Orders.GetByProductCodeAsync(productCode);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetByWeekAsync(string orderAddedDateTime)
        {
            var orders = await _unitOfWork.Orders.GetByWeekAsync(orderAddedDateTime);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<bool> IsDocumentNoUniqueAsync(string documentNo, int? excludeId = null)
        {
            return await _unitOfWork.Orders.IsDocumentNoUniqueAsync(documentNo, excludeId);
        }

        public async Task<OrderDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdAsync(id);
            if (existingOrder == null)
                throw new NotFoundException("Order", id);

            existingOrder.IsActive = isActive;
            await _unitOfWork.Orders.UpdateAsync(existingOrder);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderDto>(existingOrder);
        }

        public async Task<IEnumerable<OrderDto>> GetInactiveAsync()
        {
            var orders = await _unitOfWork.Orders.FindAsync(o => !o.IsActive);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<(IEnumerable<OrderDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null)
        {
            var (orders, totalCount) = await _unitOfWork.Orders.GetPagedAsync(
                pageNumber, 
                pageSize,
                filter: o => 
                    (string.IsNullOrEmpty(searchTerm) || o.DocumentNo.Contains(searchTerm) || o.Customer.Contains(searchTerm) || o.ProductCode.Contains(searchTerm)) &&
                    (!isActive.HasValue || o.IsActive == isActive.Value),
                orderBy: q => q.OrderByDescending(o => o.AddedDateTime)
            );

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return (orderDtos, totalCount);
        }
    }
}
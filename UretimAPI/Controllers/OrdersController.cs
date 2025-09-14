using Microsoft.AspNetCore.Mvc;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.Order;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetAll(
            [FromQuery] string status = "active")
        {
            try
            {
                IEnumerable<OrderDto> orders = status.ToLower() switch
                {
                    "active" => await _orderService.GetAllActiveAsync(),
                    "inactive" => await _orderService.GetInactiveAsync(),
                    "all" => await _orderService.GetAllAsync(),
                    _ => await _orderService.GetAllActiveAsync() // Default to active
                };

                return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResult(orders, $"Orders retrieved successfully (status: {status})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<OrderDto>>.ErrorResult("An error occurred while retrieving orders", new List<string> { ex.Message }));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(int id)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(id);
                if (order == null)
                    return NotFound(ApiResponse<OrderDto>.ErrorResult($"Order with ID {id} not found"));

                return Ok(ApiResponse<OrderDto>.SuccessResult(order, "Order retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while retrieving the order", new List<string> { ex.Message }));
            }
        }

        [HttpGet("document/{documentNo}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetByDocumentNo(string documentNo)
        {
            try
            {
                var order = await _orderService.GetByDocumentNoAsync(documentNo);
                if (order == null)
                    return NotFound(ApiResponse<OrderDto>.ErrorResult($"Order with document number '{documentNo}' not found"));

                return Ok(ApiResponse<OrderDto>.SuccessResult(order, "Order retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while retrieving the order", new List<string> { ex.Message }));
            }
        }

        [HttpGet("customer/{customer}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetByCustomer(string customer)
        {
            try
            {
                var orders = await _orderService.GetByCustomerAsync(customer);
                return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResult(orders, "Orders retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<OrderDto>>.ErrorResult("An error occurred while retrieving orders", new List<string> { ex.Message }));
            }
        }

        [HttpGet("product/{productCode}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetByProductCode(string productCode)
        {
            try
            {
                var orders = await _orderService.GetByProductCodeAsync(productCode);
                return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResult(orders, "Orders retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<OrderDto>>.ErrorResult("An error occurred while retrieving orders", new List<string> { ex.Message }));
            }
        }

        [HttpGet("week/{week}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetByWeek(string week)
        {
            try
            {
                var orders = await _orderService.GetByWeekAsync(week);
                return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResult(orders, "Orders retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<OrderDto>>.ErrorResult("An error occurred while retrieving orders", new List<string> { ex.Message }));
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetPaged(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? searchTerm = null,
            [FromQuery] string status = "active")
        {
            try
            {
                bool? isActive = status.ToLower() switch
                {
                    "active" => true,
                    "inactive" => false,
                    "all" => null,
                    _ => true // Default to active
                };

                var (items, totalCount) = await _orderService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
                
                var pagedResult = new PagedResult<OrderDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PagedResult<OrderDto>>.SuccessResult(pagedResult, $"Orders retrieved successfully (status: {status})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<OrderDto>>.ErrorResult("An error occurred while retrieving orders", new List<string> { ex.Message }));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDto>>> Create([FromBody] CreateOrderDto createDto)
        {
            try
            {
                var order = await _orderService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, 
                    ApiResponse<OrderDto>.SuccessResult(order, "Order created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<OrderDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while creating the order", new List<string> { ex.Message }));
            }
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> CreateBulk([FromBody] IEnumerable<CreateOrderDto> createDtos)
        {
            try
            {
                var orders = await _orderService.CreateBulkAsync(createDtos);
                return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResult(orders, $"{orders.Count()} orders created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<IEnumerable<OrderDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<OrderDto>>.ErrorResult("An error occurred while creating orders", new List<string> { ex.Message }));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> Update(int id, [FromBody] UpdateOrderDto updateDto)
        {
            try
            {
                var order = await _orderService.UpdateAsync(id, updateDto);
                return Ok(ApiResponse<OrderDto>.SuccessResult(order, "Order updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<OrderDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while updating the order", new List<string> { ex.Message }));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var result = await _orderService.SoftDeleteAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.ErrorResult($"Order with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResult(true, "Order deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting the order", new List<string> { ex.Message }));
            }
        }

        [HttpDelete("bulk")]
        public async Task<ActionResult<ApiResponse<bool>>> BulkDelete([FromBody] IEnumerable<int> ids)
        {
            try
            {
                var result = await _orderService.BulkSoftDeleteAsync(ids);
                return Ok(ApiResponse<bool>.SuccessResult(result, $"{ids.Count()} orders deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting orders", new List<string> { ex.Message }));
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> Activate(int id)
        {
            try
            {
                var order = await _orderService.SetActiveStatusAsync(id, true);
                return Ok(ApiResponse<OrderDto>.SuccessResult(order, "Order activated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while activating the order", new List<string> { ex.Message }));
            }
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> Deactivate(int id)
        {
            try
            {
                var order = await _orderService.SetActiveStatusAsync(id, false);
                return Ok(ApiResponse<OrderDto>.SuccessResult(order, "Order deactivated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred while deactivating the order", new List<string> { ex.Message }));
            }
        }
    }
}
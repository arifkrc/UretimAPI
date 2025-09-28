using Microsoft.AspNetCore.Mvc;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.Shipment;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentsController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;
        private readonly ILogger<ShipmentsController> _logger;

        public ShipmentsController(IShipmentService shipmentService, ILogger<ShipmentsController> logger)
        {
            _shipmentService = shipmentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ShipmentDto>>>> GetAll([FromQuery] string status = "active")
        {
            IEnumerable<ShipmentDto> shipments = status.ToLower() switch
            {
                "active" => await _shipmentService.GetAllActiveAsync(),
                "inactive" => await _shipmentService.GetInactiveAsync(),
                "all" => await _shipmentService.GetAllAsync(),
                _ => await _shipmentService.GetAllActiveAsync()
            };
            return Ok(ApiResponse<IEnumerable<ShipmentDto>>.SuccessResult(shipments, "Shipments retrieved"));
        }

        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PagedResult<ShipmentDto>>>> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _shipmentService.GetPagedAsync(pageNumber, pageSize);
            var paged = new PagedResult<ShipmentDto> { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
            return Ok(ApiResponse<PagedResult<ShipmentDto>>.SuccessResult(paged, "Shipments paged"));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ShipmentDto>>> Create([FromBody] CreateShipmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<ShipmentDto>.ErrorResult("Validation failed"));
            var created = await _shipmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<ShipmentDto>.SuccessResult(created, "Created"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ShipmentDto>>> GetById(int id)
        {
            var s = await _shipmentService.GetByIdAsync(id);
            if (s == null) return NotFound(ApiResponse<ShipmentDto>.ErrorResult($"Shipment {id} not found"));
            return Ok(ApiResponse<ShipmentDto>.SuccessResult(s, "Found"));
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ShipmentDto>>>> CreateBulk([FromBody] IEnumerable<CreateShipmentDto> dtos)
        {
            var list = dtos.ToList();
            var created = await _shipmentService.CreateBulkAsync(list);
            return Ok(ApiResponse<IEnumerable<ShipmentDto>>.SuccessResult(created, $"{created.Count()} created"));
        }

        [HttpGet("daterange")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ShipmentDto>>>> GetByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest(ApiResponse<IEnumerable<ShipmentDto>>.ErrorResult("startDate cannot be after endDate"));

            var items = await _shipmentService.GetByDateRangeAsync(startDate, endDate);
            return Ok(ApiResponse<IEnumerable<ShipmentDto>>.SuccessResult(items, "Shipments retrieved by date range"));
        }
    }
}

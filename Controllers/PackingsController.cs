using Microsoft.AspNetCore.Mvc;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.Packing;
using UretimAPI.Services.Interfaces;

namespace UretimAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackingsController : ControllerBase
    {
        private readonly IPackingService _packingService;

        public PackingsController(IPackingService packingService)
        {
            _packingService = packingService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<PackingDto>>>> GetAll(
            [FromQuery] string status = "active")
        {
            try
            {
                IEnumerable<PackingDto> packings = status.ToLower() switch
                {
                    "active" => await _packingService.GetAllActiveAsync(),
                    "inactive" => await _packingService.GetInactiveAsync(),
                    "all" => await _packingService.GetAllAsync(),
                    _ => await _packingService.GetAllActiveAsync() // Default to active
                };

                return Ok(ApiResponse<IEnumerable<PackingDto>>.SuccessResult(packings, $"Packings retrieved successfully (status: {status})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<PackingDto>>.ErrorResult("An error occurred while retrieving packings", new List<string> { ex.Message }));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PackingDto>>> GetById(int id)
        {
            try
            {
                var packing = await _packingService.GetByIdAsync(id);
                if (packing == null)
                    return NotFound(ApiResponse<PackingDto>.ErrorResult($"Packing with ID {id} not found"));

                return Ok(ApiResponse<PackingDto>.SuccessResult(packing, "Packing retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PackingDto>.ErrorResult("An error occurred while retrieving the packing", new List<string> { ex.Message }));
            }
        }

        [HttpGet("product/{productCode}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PackingDto>>>> GetByProductCode(string productCode)
        {
            try
            {
                var packings = await _packingService.GetByProductCodeAsync(productCode);
                return Ok(ApiResponse<IEnumerable<PackingDto>>.SuccessResult(packings, "Packings retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<PackingDto>>.ErrorResult("An error occurred while retrieving packings", new List<string> { ex.Message }));
            }
        }

        [HttpGet("daterange")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PackingDto>>>> GetByDateRange(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            try
            {
                var packings = await _packingService.GetByDateRangeAsync(startDate, endDate);
                return Ok(ApiResponse<IEnumerable<PackingDto>>.SuccessResult(packings, "Packings retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<PackingDto>>.ErrorResult("An error occurred while retrieving packings", new List<string> { ex.Message }));
            }
        }

        [HttpGet("shift/{shift}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PackingDto>>>> GetByShift(
            string shift, 
            [FromQuery] DateTime date)
        {
            try
            {
                var packings = await _packingService.GetByShiftAsync(shift, date);
                return Ok(ApiResponse<IEnumerable<PackingDto>>.SuccessResult(packings, "Packings retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<PackingDto>>.ErrorResult("An error occurred while retrieving packings", new List<string> { ex.Message }));
            }
        }

        [HttpGet("totalquantity/{productCode}")]
        public async Task<ActionResult<ApiResponse<int>>> GetTotalPackedQuantity(
            string productCode,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var totalQuantity = await _packingService.GetTotalPackedQuantityAsync(productCode, startDate, endDate);
                return Ok(ApiResponse<int>.SuccessResult(totalQuantity, "Total packed quantity retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<int>.ErrorResult("An error occurred while calculating total packed quantity", new List<string> { ex.Message }));
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PagedResult<PackingDto>>>> GetPaged(
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

                var (items, totalCount) = await _packingService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
                
                var pagedResult = new PagedResult<PackingDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PagedResult<PackingDto>>.SuccessResult(pagedResult, $"Packings retrieved successfully (status: {status})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<PackingDto>>.ErrorResult("An error occurred while retrieving packings", new List<string> { ex.Message }));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<PackingDto>>> Create([FromBody] CreatePackingDto createDto)
        {
            try
            {
                var packing = await _packingService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = packing.Id }, 
                    ApiResponse<PackingDto>.SuccessResult(packing, "Packing created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PackingDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PackingDto>.ErrorResult("An error occurred while creating the packing", new List<string> { ex.Message }));
            }
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PackingDto>>>> CreateBulk([FromBody] IEnumerable<CreatePackingDto> createDtos)
        {
            try
            {
                var packings = await _packingService.CreateBulkAsync(createDtos);
                return Ok(ApiResponse<IEnumerable<PackingDto>>.SuccessResult(packings, $"{packings.Count()} packings created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<IEnumerable<PackingDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<PackingDto>>.ErrorResult("An error occurred while creating packings", new List<string> { ex.Message }));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PackingDto>>> Update(int id, [FromBody] UpdatePackingDto updateDto)
        {
            try
            {
                var packing = await _packingService.UpdateAsync(id, updateDto);
                return Ok(ApiResponse<PackingDto>.SuccessResult(packing, "Packing updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PackingDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PackingDto>.ErrorResult("An error occurred while updating the packing", new List<string> { ex.Message }));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var result = await _packingService.SoftDeleteAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.ErrorResult($"Packing with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResult(true, "Packing deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting the packing", new List<string> { ex.Message }));
            }
        }

        [HttpDelete("bulk")]
        public async Task<ActionResult<ApiResponse<bool>>> BulkDelete([FromBody] IEnumerable<int> ids)
        {
            try
            {
                var result = await _packingService.BulkSoftDeleteAsync(ids);
                return Ok(ApiResponse<bool>.SuccessResult(result, $"{ids.Count()} packings deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting packings", new List<string> { ex.Message }));
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<ActionResult<ApiResponse<PackingDto>>> Activate(int id)
        {
            try
            {
                var packing = await _packingService.SetActiveStatusAsync(id, true);
                return Ok(ApiResponse<PackingDto>.SuccessResult(packing, "Packing activated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PackingDto>.ErrorResult("An error occurred while activating the packing", new List<string> { ex.Message }));
            }
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<PackingDto>>> Deactivate(int id)
        {
            try
            {
                var packing = await _packingService.SetActiveStatusAsync(id, false);
                return Ok(ApiResponse<PackingDto>.SuccessResult(packing, "Packing deactivated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PackingDto>.ErrorResult("An error occurred while deactivating the packing", new List<string> { ex.Message }));
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.ProductionTrackingForm;
using UretimAPI.Services.Interfaces;
using UretimAPI.Services.Caching;
using UretimAPI.Configuration;
using Microsoft.Extensions.Options;

namespace UretimAPI.Controllers
{
    /// <summary>
    /// Production tracking forms management controller for handling production tracking data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductionTrackingFormsController : ControllerBase
    {
        private readonly IProductionTrackingFormService _ptfService;
        private readonly ICacheService _cacheService;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<ProductionTrackingFormsController> _logger;

        public ProductionTrackingFormsController(
            IProductionTrackingFormService ptfService,
            ICacheService cacheService,
            IOptions<ApiSettings> apiSettings,
            ILogger<ProductionTrackingFormsController> logger)
        {
            _ptfService = ptfService;
            _cacheService = cacheService;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all production tracking forms with optional status filtering
        /// </summary>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>List of production tracking forms based on status filter</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductionTrackingFormDto>>>> GetAll(
            [FromQuery] string status = "active")
        {
            _logger.LogInformation("Retrieving production tracking forms with status: {Status}", status);
            
            var cacheKey = $"ptf:all:{status}";
            
            var cachedPtfs = await _cacheService.GetAsync<IEnumerable<ProductionTrackingFormDto>>(cacheKey);
            if (cachedPtfs != null)
            {
                _logger.LogDebug("Production tracking forms retrieved from cache");
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Status-Filter", status);
                return Ok(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.SuccessResult(cachedPtfs, "Production tracking forms retrieved successfully (cached)"));
            }

            IEnumerable<ProductionTrackingFormDto> ptfs = status.ToLower() switch
            {
                "active" => await _ptfService.GetAllActiveAsync(),
                "inactive" => await _ptfService.GetInactiveAsync(),
                "all" => await _ptfService.GetAllAsync(),
                _ => await _ptfService.GetAllActiveAsync() // Default to active
            };

            await _cacheService.SetAsync(cacheKey, ptfs, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Retrieved {Count} production tracking forms with status {Status}", ptfs.Count(), status);
            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", ptfs.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.SuccessResult(ptfs, "Production tracking forms retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a production tracking form by its ID
        /// </summary>
        /// <param name="id">Production tracking form ID</param>
        /// <returns>Production tracking form details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 400)]
        public async Task<ActionResult<ApiResponse<ProductionTrackingFormDto>>> GetById([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Retrieving production tracking form with ID: {PtfId}", id);
            
            var cacheKey = $"ptf:id:{id}";
            var cachedPtf = await _cacheService.GetAsync<ProductionTrackingFormDto>(cacheKey);
            if (cachedPtf != null)
            {
                _logger.LogDebug("Production tracking form {PtfId} retrieved from cache", id);
                Response.Headers.Add("X-Cache", "HIT");
                return Ok(ApiResponse<ProductionTrackingFormDto>.SuccessResult(cachedPtf, "Production tracking form retrieved successfully (cached)"));
            }

            var ptf = await _ptfService.GetByIdAsync(id);
            if (ptf == null)
            {
                _logger.LogWarning("Production tracking form with ID {PtfId} not found", id);
                return NotFound(ApiResponse<ProductionTrackingFormDto>.ErrorResult($"Production tracking form with ID {id} not found"));
            }

            await _cacheService.SetAsync(cacheKey, ptf, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Production tracking form {PtfId} retrieved successfully", id);
            Response.Headers.Add("X-Cache", "MISS");
            
            return Ok(ApiResponse<ProductionTrackingFormDto>.SuccessResult(ptf, "Production tracking form retrieved successfully"));
        }

        /// <summary>
        /// Retrieves production tracking forms by product code
        /// </summary>
        /// <param name="productCode">Product code</param>
        /// <returns>Production tracking forms for the product</returns>
        [HttpGet("product/{productCode}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 400)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductionTrackingFormDto>>>> GetByProductCode(
            [FromRoute][Required][StringLength(50, MinimumLength = 1)] string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                _logger.LogWarning("Invalid product code parameter: empty or whitespace");
                return BadRequest(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.ErrorResult("Product code cannot be empty"));
            }

            _logger.LogInformation("Retrieving production tracking forms for product: {ProductCode}", productCode);
            
            var cacheKey = CacheKeys.ProductionTrackingByDateRange(DateTime.Today.AddDays(-30), DateTime.Today);
            var cachedPtfs = await _cacheService.GetAsync<IEnumerable<ProductionTrackingFormDto>>(cacheKey);
            
            if (cachedPtfs == null)
            {
                var ptfs = await _ptfService.GetByProductCodeAsync(productCode);
                await _cacheService.SetAsync(cacheKey, ptfs, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
                cachedPtfs = ptfs;
                Response.Headers.Add("X-Cache", "MISS");
            }
            else
            {
                Response.Headers.Add("X-Cache", "HIT");
            }

            _logger.LogInformation("Retrieved production tracking forms for product {ProductCode}", productCode);
            Response.Headers.Add("X-Total-Count", cachedPtfs.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.SuccessResult(cachedPtfs, "Production tracking forms retrieved successfully"));
        }

        /// <summary>
        /// Retrieves production tracking forms with pagination and optional status filtering
        /// </summary>
        /// <param name="pageNumber">Page number (minimum 1)</param>
        /// <param name="pageSize">Page size (0 for default, maximum from configuration)</param>
        /// <param name="searchTerm">Search term for filtering (maximum 100 characters)</param>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>Paginated list of production tracking forms</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductionTrackingFormDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductionTrackingFormDto>>), 400)]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductionTrackingFormDto>>>> GetPaged(
            [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1, 
            [FromQuery][Range(0, 1000)] int pageSize = 0, 
            [FromQuery][StringLength(100)] string? searchTerm = null,
            [FromQuery] string status = "active")
        {
            try
            {
                if (pageNumber < 1)
                {
                    _logger.LogWarning("Invalid page number: {PageNumber}", pageNumber);
                    return BadRequest(ApiResponse<PagedResult<ProductionTrackingFormDto>>.ErrorResult("Page number must be greater than 0"));
                }

                if (pageSize <= 0) pageSize = _apiSettings.DefaultPageSize;
                if (pageSize > _apiSettings.MaxPageSize) pageSize = _apiSettings.MaxPageSize;

                _logger.LogInformation("Retrieving paged production tracking forms - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}, Status: {Status}", 
                    pageNumber, pageSize, searchTerm ?? "none", status);

                bool? isActive = status.ToLower() switch
                {
                    "active" => true,
                    "inactive" => false,
                    "all" => null,
                    _ => true // Default to active
                };

                var (items, totalCount) = await _ptfService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
                
                var pagedResult = new PagedResult<ProductionTrackingFormDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {ItemCount} production tracking forms from page {PageNumber} (total: {TotalCount}) with status {Status}", 
                    items.Count(), pageNumber, totalCount, status);

                Response.Headers.Add("X-Cache", "MISS");
                Response.Headers.Add("X-Status-Filter", status);
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page-Number", pageNumber.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

                return Ok(ApiResponse<PagedResult<ProductionTrackingFormDto>>.SuccessResult(pagedResult, "Production tracking forms retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting paged production tracking forms");
                var errors = new List<string> { ex.Message };
                if (ex.InnerException != null) errors.Add(ex.InnerException.Message);
                return StatusCode(500, ApiResponse<PagedResult<ProductionTrackingFormDto>>.ErrorResult("Internal server error", errors));
            }
        }

        /// <summary>
        /// Creates a new production tracking form
        /// </summary>
        /// <param name="createDto">Production tracking form creation data</param>
        /// <returns>Created production tracking form</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductionTrackingFormDto>>> Create([FromBody] CreateProductionTrackingFormDto createDto)
        {
            if (createDto == null)
            {
                _logger.LogWarning("Create production tracking form called with null data");
                return BadRequest(ApiResponse<ProductionTrackingFormDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Production tracking form creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<ProductionTrackingFormDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating new production tracking form for product: {ProductCode}", createDto.ProductCode);

            var ptf = await _ptfService.CreateAsync(createDto);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Production tracking form created successfully with ID: {PtfId}", ptf.Id);
            
            return CreatedAtAction(nameof(GetById), new { id = ptf.Id }, 
                ApiResponse<ProductionTrackingFormDto>.SuccessResult(ptf, "Production tracking form created successfully"));
        }

        /// <summary>
        /// Creates multiple production tracking forms in bulk
        /// </summary>
        /// <param name="createDtos">List of production tracking form creation data</param>
        /// <returns>Created production tracking forms</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 400)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductionTrackingFormDto>>), 404)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductionTrackingFormDto>>>> CreateBulk([FromBody] IEnumerable<CreateProductionTrackingFormDto> createDtos)
        {
            if (createDtos == null)
            {
                _logger.LogWarning("Bulk create production tracking forms called with null data");
                return BadRequest(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.ErrorResult("Request body cannot be null"));
            }

            var createDtosList = createDtos.ToList();
            
            if (createDtosList.Count == 0)
            {
                _logger.LogWarning("Bulk create production tracking forms called with empty list");
                return BadRequest(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.ErrorResult("At least one production tracking form must be provided"));
            }
            
            if (createDtosList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk operation limit exceeded: {Count} production tracking forms, limit: {Limit}", 
                    createDtosList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {createDtosList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Bulk production tracking form creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating {Count} production tracking forms in bulk", createDtosList.Count);

            var ptfs = await _ptfService.CreateBulkAsync(createDtosList);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Successfully created {Count} production tracking forms in bulk", ptfs.Count());
            
            return Ok(ApiResponse<IEnumerable<ProductionTrackingFormDto>>.SuccessResult(ptfs, $"{ptfs.Count()} production tracking forms created successfully"));
        }

        /// <summary>
        /// Updates an existing production tracking form
        /// </summary>
        /// <param name="id">Production tracking form ID</param>
        /// <param name="updateDto">Production tracking form update data</param>
        /// <returns>Updated production tracking form</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductionTrackingFormDto>>> Update(
            [FromRoute][Range(1, int.MaxValue)] int id, 
            [FromBody] UpdateProductionTrackingFormDto updateDto)
        {
            if (updateDto == null)
            {
                _logger.LogWarning("Update production tracking form {PtfId} called with null data", id);
                return BadRequest(ApiResponse<ProductionTrackingFormDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Production tracking form {PtfId} update validation failed: {Errors}", id, string.Join(", ", errors));
                return BadRequest(ApiResponse<ProductionTrackingFormDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Updating production tracking form {PtfId}", id);

            var ptf = await _ptfService.UpdateAsync(id, updateDto);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Production tracking form {PtfId} updated successfully", id);
            
            return Ok(ApiResponse<ProductionTrackingFormDto>.SuccessResult(ptf, "Production tracking form updated successfully"));
        }

        /// <summary>
        /// Soft deletes a production tracking form
        /// </summary>
        /// <param name="id">Production tracking form ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deleting production tracking form {PtfId}", id);
            
            var result = await _ptfService.SoftDeleteAsync(id);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Production tracking form {PtfId} deleted successfully", id);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Production tracking form deleted successfully"));
        }

        /// <summary>
        /// Soft deletes multiple production tracking forms in bulk
        /// </summary>
        /// <param name="ids">List of production tracking form IDs</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> BulkDelete([FromBody] IEnumerable<int> ids)
        {
            if (ids == null)
            {
                _logger.LogWarning("Bulk delete production tracking forms called with null data");
                return BadRequest(ApiResponse<bool>.ErrorResult("Request body cannot be null"));
            }

            var idsList = ids.ToList();
            
            if (idsList.Count == 0)
            {
                _logger.LogWarning("Bulk delete production tracking forms called with empty list");
                return BadRequest(ApiResponse<bool>.ErrorResult("At least one production tracking form ID must be provided"));
            }
            
            if (idsList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk delete limit exceeded: {Count} production tracking forms, limit: {Limit}", 
                    idsList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<bool>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {idsList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (idsList.Any(id => id <= 0))
            {
                _logger.LogWarning("Bulk delete contains invalid IDs: {InvalidIds}", 
                    string.Join(", ", idsList.Where(id => id <= 0)));
                return BadRequest(ApiResponse<bool>.ErrorResult("All production tracking form IDs must be positive integers"));
            }

            _logger.LogInformation("Bulk deleting {Count} production tracking forms", idsList.Count);

            var result = await _ptfService.BulkSoftDeleteAsync(idsList);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Successfully deleted {Count} production tracking forms in bulk", idsList.Count);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, $"{idsList.Count} production tracking forms deleted successfully"));
        }

        /// <summary>
        /// Activates a production tracking form
        /// </summary>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductionTrackingFormDto>>> Activate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Activating production tracking form {PtfId}", id);
            
            var ptf = await _ptfService.SetActiveStatusAsync(id, true);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Production tracking form {PtfId} activated successfully", id);
            
            return Ok(ApiResponse<ProductionTrackingFormDto>.SuccessResult(ptf, "Production tracking form activated successfully"));
        }

        /// <summary>
        /// Deactivates a production tracking form
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductionTrackingFormDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductionTrackingFormDto>>> Deactivate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deactivating production tracking form {PtfId}", id);
            
            var ptf = await _ptfService.SetActiveStatusAsync(id, false);

            await _cacheService.RemoveByPatternAsync("ptf:");

            _logger.LogInformation("Production tracking form {PtfId} deactivated successfully", id);
            
            return Ok(ApiResponse<ProductionTrackingFormDto>.SuccessResult(ptf, "Production tracking form deactivated successfully"));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.CycleTime;
using UretimAPI.Services.Interfaces;
using UretimAPI.Services.Caching;
using UretimAPI.Configuration;
using Microsoft.Extensions.Options;

namespace UretimAPI.Controllers
{
    /// <summary>
    /// Cycle times management controller for handling production cycle times
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CycleTimesController : ControllerBase
    {
        private readonly ICycleTimeService _cycleTimeService;
        private readonly ICacheService _cacheService;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<CycleTimesController> _logger;

        public CycleTimesController(
            ICycleTimeService cycleTimeService,
            ICacheService cacheService,
            IOptions<ApiSettings> apiSettings,
            ILogger<CycleTimesController> logger)
        {
            _cycleTimeService = cycleTimeService;
            _cacheService = cacheService;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all cycle times with optional status filtering
        /// </summary>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>List of cycle times based on status filter</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CycleTimeDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CycleTimeDto>>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<CycleTimeDto>>>> GetAll(
            [FromQuery] string status = "active")
        {
            _logger.LogInformation("Retrieving cycle times with status: {Status}", status);
            
            var cacheKey = $"cycletimes:all:{status}";
            
            var cachedCycleTimes = await _cacheService.GetAsync<IEnumerable<CycleTimeDto>>(cacheKey);
            if (cachedCycleTimes != null)
            {
                _logger.LogDebug("Cycle times retrieved from cache");
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Status-Filter", status);
                return Ok(ApiResponse<IEnumerable<CycleTimeDto>>.SuccessResult(cachedCycleTimes, "Cycle times retrieved successfully (cached)"));
            }

            IEnumerable<CycleTimeDto> cycleTimes = status.ToLower() switch
            {
                "active" => await _cycleTimeService.GetAllActiveAsync(),
                "inactive" => await _cycleTimeService.GetInactiveAsync(),
                "all" => await _cycleTimeService.GetAllAsync(),
                _ => await _cycleTimeService.GetAllActiveAsync() // Default to active
            };

            await _cacheService.SetAsync(cacheKey, cycleTimes, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Retrieved {Count} cycle times with status {Status}", cycleTimes.Count(), status);
            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", cycleTimes.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<CycleTimeDto>>.SuccessResult(cycleTimes, "Cycle times retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a cycle time by its ID
        /// </summary>
        /// <param name="id">Cycle time ID</param>
        /// <returns>Cycle time details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 400)]
        public async Task<ActionResult<ApiResponse<CycleTimeDto>>> GetById([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Retrieving cycle time with ID: {CycleTimeId}", id);
            
            var cacheKey = $"cycletime:id:{id}";
            var cachedCycleTime = await _cacheService.GetAsync<CycleTimeDto>(cacheKey);
            if (cachedCycleTime != null)
            {
                _logger.LogDebug("Cycle time {CycleTimeId} retrieved from cache", id);
                Response.Headers.Add("X-Cache", "HIT");
                return Ok(ApiResponse<CycleTimeDto>.SuccessResult(cachedCycleTime, "Cycle time retrieved successfully (cached)"));
            }

            var cycleTime = await _cycleTimeService.GetByIdAsync(id);
            if (cycleTime == null)
            {
                _logger.LogWarning("Cycle time with ID {CycleTimeId} not found", id);
                return NotFound(ApiResponse<CycleTimeDto>.ErrorResult($"Cycle time with ID {id} not found"));
            }

            await _cacheService.SetAsync(cacheKey, cycleTime, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Cycle time {CycleTimeId} retrieved successfully", id);
            Response.Headers.Add("X-Cache", "MISS");
            
            return Ok(ApiResponse<CycleTimeDto>.SuccessResult(cycleTime, "Cycle time retrieved successfully"));
        }

        /// <summary>
        /// Retrieves cycle times with pagination and optional status filtering
        /// </summary>
        /// <param name="pageNumber">Page number (minimum 1)</param>
        /// <param name="pageSize">Page size (0 for default, maximum from configuration)</param>
        /// <param name="searchTerm">Search term for filtering (maximum 100 characters)</param>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>Paginated list of cycle times</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CycleTimeDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CycleTimeDto>>), 400)]
        public async Task<ActionResult<ApiResponse<PagedResult<CycleTimeDto>>>> GetPaged(
            [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1, 
            [FromQuery][Range(0, 1000)] int pageSize = 0, 
            [FromQuery][StringLength(100)] string? searchTerm = null,
            [FromQuery] string status = "active")
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Invalid page number: {PageNumber}", pageNumber);
                return BadRequest(ApiResponse<PagedResult<CycleTimeDto>>.ErrorResult("Page number must be greater than 0"));
            }

            if (pageSize <= 0) pageSize = _apiSettings.DefaultPageSize;
            if (pageSize > _apiSettings.MaxPageSize) pageSize = _apiSettings.MaxPageSize;

            _logger.LogInformation("Retrieving paged cycle times - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}, Status: {Status}", 
                pageNumber, pageSize, searchTerm ?? "none", status);

            var cacheKey = $"cycletimes:page:{pageNumber}:size:{pageSize}:search:{searchTerm ?? "all"}:status:{status}";
            var cachedResult = await _cacheService.GetAsync<PagedResult<CycleTimeDto>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Paged cycle times retrieved from cache");
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Status-Filter", status);
                Response.Headers.Add("X-Total-Count", cachedResult.TotalCount.ToString());
                Response.Headers.Add("X-Page-Number", pageNumber.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", cachedResult.TotalPages.ToString());
                return Ok(ApiResponse<PagedResult<CycleTimeDto>>.SuccessResult(cachedResult, "Cycle times retrieved successfully (cached)"));
            }

            bool? isActive = status.ToLower() switch
            {
                "active" => true,
                "inactive" => false,
                "all" => null,
                _ => true // Default to active
            };

            var (items, totalCount) = await _cycleTimeService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
            
            var pagedResult = new PagedResult<CycleTimeDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            await _cacheService.SetAsync(cacheKey, pagedResult, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes / 2));

            _logger.LogInformation("Retrieved {ItemCount} cycle times from page {PageNumber} (total: {TotalCount}) with status {Status}", 
                items.Count(), pageNumber, totalCount, status);

            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page-Number", pageNumber.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(ApiResponse<PagedResult<CycleTimeDto>>.SuccessResult(pagedResult, "Cycle times retrieved successfully"));
        }

        /// <summary>
        /// Creates a new cycle time
        /// </summary>
        /// <param name="createDto">Cycle time creation data</param>
        /// <returns>Created cycle time</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 404)]
        public async Task<ActionResult<ApiResponse<CycleTimeDto>>> Create([FromBody] CreateCycleTimeDto createDto)
        {
            if (createDto == null)
            {
                _logger.LogWarning("Create cycle time called with null data");
                return BadRequest(ApiResponse<CycleTimeDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Cycle time creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<CycleTimeDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating new cycle time for product: {ProductId}, operation: {OperationId}", 
                createDto.ProductId, createDto.OperationId);

            var cycleTime = await _cycleTimeService.CreateAsync(createDto);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Cycle time created successfully with ID: {CycleTimeId}", cycleTime.Id);
            
            return CreatedAtAction(nameof(GetById), new { id = cycleTime.Id }, 
                ApiResponse<CycleTimeDto>.SuccessResult(cycleTime, "Cycle time created successfully"));
        }

        /// <summary>
        /// Creates multiple cycle times in bulk
        /// </summary>
        /// <param name="createDtos">List of cycle time creation data</param>
        /// <returns>Created cycle times</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CycleTimeDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CycleTimeDto>>), 400)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CycleTimeDto>>), 404)]
        public async Task<ActionResult<ApiResponse<IEnumerable<CycleTimeDto>>>> CreateBulk([FromBody] IEnumerable<CreateCycleTimeDto> createDtos)
        {
            if (createDtos == null)
            {
                _logger.LogWarning("Bulk create cycle times called with null data");
                return BadRequest(ApiResponse<IEnumerable<CycleTimeDto>>.ErrorResult("Request body cannot be null"));
            }

            var createDtosList = createDtos.ToList();
            
            if (createDtosList.Count == 0)
            {
                _logger.LogWarning("Bulk create cycle times called with empty list");
                return BadRequest(ApiResponse<IEnumerable<CycleTimeDto>>.ErrorResult("At least one cycle time must be provided"));
            }
            
            if (createDtosList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk operation limit exceeded: {Count} cycle times, limit: {Limit}", 
                    createDtosList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<IEnumerable<CycleTimeDto>>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {createDtosList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Bulk cycle time creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<IEnumerable<CycleTimeDto>>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating {Count} cycle times in bulk", createDtosList.Count);

            var cycleTimes = await _cycleTimeService.CreateBulkAsync(createDtosList);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Successfully created {Count} cycle times in bulk", cycleTimes.Count());
            
            return Ok(ApiResponse<IEnumerable<CycleTimeDto>>.SuccessResult(cycleTimes, $"{cycleTimes.Count()} cycle times created successfully"));
        }

        /// <summary>
        /// Updates an existing cycle time
        /// </summary>
        /// <param name="id">Cycle time ID</param>
        /// <param name="updateDto">Cycle time update data</param>
        /// <returns>Updated cycle time</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 404)]
        public async Task<ActionResult<ApiResponse<CycleTimeDto>>> Update(
            [FromRoute][Range(1, int.MaxValue)] int id, 
            [FromBody] UpdateCycleTimeDto updateDto)
        {
            if (updateDto == null)
            {
                _logger.LogWarning("Update cycle time {CycleTimeId} called with null data", id);
                return BadRequest(ApiResponse<CycleTimeDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Cycle time {CycleTimeId} update validation failed: {Errors}", id, string.Join(", ", errors));
                return BadRequest(ApiResponse<CycleTimeDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Updating cycle time {CycleTimeId}", id);

            var cycleTime = await _cycleTimeService.UpdateAsync(id, updateDto);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Cycle time {CycleTimeId} updated successfully", id);
            
            return Ok(ApiResponse<CycleTimeDto>.SuccessResult(cycleTime, "Cycle time updated successfully"));
        }

        /// <summary>
        /// Soft deletes a cycle time
        /// </summary>
        /// <param name="id">Cycle time ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deleting cycle time {CycleTimeId}", id);
            
            var result = await _cycleTimeService.SoftDeleteAsync(id);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Cycle time {CycleTimeId} deleted successfully", id);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Cycle time deleted successfully"));
        }

        /// <summary>
        /// Soft deletes multiple cycle times in bulk
        /// </summary>
        /// <param name="ids">List of cycle time IDs</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> BulkDelete([FromBody] IEnumerable<int> ids)
        {
            if (ids == null)
            {
                _logger.LogWarning("Bulk delete cycle times called with null data");
                return BadRequest(ApiResponse<bool>.ErrorResult("Request body cannot be null"));
            }

            var idsList = ids.ToList();
            
            if (idsList.Count == 0)
            {
                _logger.LogWarning("Bulk delete cycle times called with empty list");
                return BadRequest(ApiResponse<bool>.ErrorResult("At least one cycle time ID must be provided"));
            }
            
            if (idsList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk delete limit exceeded: {Count} cycle times, limit: {Limit}", 
                    idsList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<bool>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {idsList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (idsList.Any(id => id <= 0))
            {
                _logger.LogWarning("Bulk delete contains invalid IDs: {InvalidIds}", 
                    string.Join(", ", idsList.Where(id => id <= 0)));
                return BadRequest(ApiResponse<bool>.ErrorResult("All cycle time IDs must be positive integers"));
            }

            _logger.LogInformation("Bulk deleting {Count} cycle times", idsList.Count);

            var result = await _cycleTimeService.BulkSoftDeleteAsync(idsList);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Successfully deleted {Count} cycle times in bulk", idsList.Count);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, $"{idsList.Count} cycle times deleted successfully"));
        }

        /// <summary>
        /// Activates a cycle time
        /// </summary>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 404)]
        public async Task<ActionResult<ApiResponse<CycleTimeDto>>> Activate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Activating cycle time {CycleTimeId}", id);
            
            var cycleTime = await _cycleTimeService.SetActiveStatusAsync(id, true);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Cycle time {CycleTimeId} activated successfully", id);
            
            return Ok(ApiResponse<CycleTimeDto>.SuccessResult(cycleTime, "Cycle time activated successfully"));
        }

        /// <summary>
        /// Deactivates a cycle time
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<CycleTimeDto>), 404)]
        public async Task<ActionResult<ApiResponse<CycleTimeDto>>> Deactivate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deactivating cycle time {CycleTimeId}", id);
            
            var cycleTime = await _cycleTimeService.SetActiveStatusAsync(id, false);

            await _cacheService.RemoveByPatternAsync("cycletimes:");

            _logger.LogInformation("Cycle time {CycleTimeId} deactivated successfully", id);
            
            return Ok(ApiResponse<CycleTimeDto>.SuccessResult(cycleTime, "Cycle time deactivated successfully"));
        }
    }
}
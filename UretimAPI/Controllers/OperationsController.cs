using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.Operation;
using UretimAPI.Services.Interfaces;
using UretimAPI.Services.Caching;
using UretimAPI.Configuration;
using Microsoft.Extensions.Options;

namespace UretimAPI.Controllers
{
    /// <summary>
    /// Operations management controller for handling production operations
    /// Optimized for dual client architecture (Reporting + Data Entry)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationService _operationService;
        private readonly ICacheService _cacheService;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(
            IOperationService operationService, 
            ICacheService cacheService,
            IOptions<ApiSettings> apiSettings,
            ILogger<OperationsController> logger)
        {
            _operationService = operationService;
            _cacheService = cacheService;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        #region REPORTING CLIENT OPTIMIZED ENDPOINTS

        /// <summary>
        /// [REPORTING CLIENT] Retrieves operations optimized for reporting with extended caching
        /// </summary>
        /// <param name="clientType">Client type for optimization (report/entry)</param>
        /// <param name="status">Operation status filter (active/inactive/all)</param>
        /// <returns>List of operations based on status filter</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OperationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<OperationDto>>>> GetAll(
            [FromQuery] string clientType = "report",
            [FromQuery] string status = "active")
        {
            _logger.LogInformation("Retrieving operations for client type: {ClientType}, status: {Status}", clientType, status);
            
            // Different cache strategies for different clients and status
            var cacheKey = clientType == "report" 
                ? $"operations:all:{status}:report" 
                : $"operations:all:{status}:entry";
                
            var cacheExpiration = clientType == "report" 
                ? TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes * 2) // Longer cache for reports
                : TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes / 2); // Shorter cache for data entry
            
            var cachedOperations = await _cacheService.GetAsync<IEnumerable<OperationDto>>(cacheKey);
            if (cachedOperations != null)
            {
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Client-Type", clientType);
                Response.Headers.Add("X-Status-Filter", status);
                return Ok(ApiResponse<IEnumerable<OperationDto>>.SuccessResult(cachedOperations, 
                    $"Operations retrieved successfully (cached for {clientType})"));
            }

            IEnumerable<OperationDto> operations = status.ToLower() switch
            {
                "active" => await _operationService.GetAllActiveAsync(),
                "inactive" => await _operationService.GetInactiveAsync(),
                "all" => await _operationService.GetAllAsync(),
                _ => await _operationService.GetAllActiveAsync() // Default to active
            };

            await _cacheService.SetAsync(cacheKey, operations, cacheExpiration);
            
            _logger.LogInformation("Retrieved {Count} operations for {ClientType} client with status {Status}", 
                operations.Count(), clientType, status);
            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Client-Type", clientType);
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", operations.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<OperationDto>>.SuccessResult(operations, 
                $"Operations retrieved successfully for {clientType} client"));
        }

        /// <summary>
        /// [REPORTING CLIENT] Optimized pagination with larger page sizes for reports
        /// </summary>
        [HttpGet("report/paged")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<OperationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<OperationDto>>>> GetPagedForReporting(
            [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1, 
            [FromQuery][Range(0, 500)] int pageSize = 0, // Larger max for reports
            [FromQuery][StringLength(100)] string? searchTerm = null,
            [FromQuery] string status = "active") // active, inactive, all
        {
            // Optimize for reporting: larger default page size
            if (pageSize <= 0) pageSize = Math.Min(_apiSettings.DefaultPageSize * 2, 100);
            if (pageSize > 500) pageSize = 500; // Reports can handle larger pages

            _logger.LogInformation("Retrieving paged operations for REPORTING - Page: {PageNumber}, Size: {PageSize}, Status: {Status}", 
                pageNumber, pageSize, status);

            var cacheKey = $"operations:report:page:{pageNumber}:size:{pageSize}:search:{searchTerm ?? "all"}:status:{status}";
            var cachedResult = await _cacheService.GetAsync<PagedResult<OperationDto>>(cacheKey);
            
            if (cachedResult != null)
            {
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Client-Type", "report");
                Response.Headers.Add("X-Status-Filter", status);
                Response.Headers.Add("X-Total-Count", cachedResult.TotalCount.ToString());
                Response.Headers.Add("X-Page-Number", pageNumber.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", cachedResult.TotalPages.ToString());
                return Ok(ApiResponse<PagedResult<OperationDto>>.SuccessResult(cachedResult, 
                    "Operations retrieved successfully (cached for reporting)"));
            }

            bool? isActive = status.ToLower() switch
            {
                "active" => true,
                "inactive" => false,
                "all" => null,
                _ => true // Default to active
            };

            var (items, totalCount) = await _operationService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
            
            var pagedResult = new PagedResult<OperationDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Longer cache for reporting data
            await _cacheService.SetAsync(cacheKey, pagedResult, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes * 2));

            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Client-Type", "report");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page-Number", pageNumber.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(ApiResponse<PagedResult<OperationDto>>.SuccessResult(pagedResult, 
                "Operations retrieved successfully for reporting"));
        }

        /// <summary>
        /// [REPORTING CLIENT] Lightweight operations list for dropdowns/selects
        /// </summary>
        [HttpGet("report/lightweight")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetLightweightForReporting()
        {
            _logger.LogInformation("Retrieving lightweight operations for reporting dropdowns");
            
            const string cacheKey = "operations:lightweight:report";
            var cached = await _cacheService.GetAsync<IEnumerable<object>>(cacheKey);
            
            if (cached != null)
            {
                Response.Headers.Add("X-Cache", "HIT");
                return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(cached, 
                    "Lightweight operations retrieved (cached)"));
            }

            var operations = await _operationService.GetAllActiveAsync();
            var lightweight = operations.Select(o => new { o.Id, o.Name, o.ShortCode }).ToList();
            
            // Very long cache for dropdown data (30 minutes)
            await _cacheService.SetAsync(cacheKey, lightweight, TimeSpan.FromMinutes(30));
            
            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Total-Count", lightweight.Count.ToString());
            
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(lightweight, 
                "Lightweight operations retrieved successfully"));
        }

        #endregion

        #region DATA ENTRY CLIENT OPTIMIZED ENDPOINTS

        /// <summary>
        /// [DATA ENTRY CLIENT] Fast retrieval with minimal caching for real-time data
        /// </summary>
        [HttpGet("entry/paged")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<OperationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<OperationDto>>>> GetPagedForDataEntry(
            [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1, 
            [FromQuery][Range(0, 100)] int pageSize = 0, // Smaller max for data entry
            [FromQuery][StringLength(100)] string? searchTerm = null,
            [FromQuery] string status = "active") // active, inactive, all
        {
            // Optimize for data entry: smaller page sizes, faster responses
            if (pageSize <= 0) pageSize = _apiSettings.DefaultPageSize;
            if (pageSize > _apiSettings.MaxPageSize) pageSize = _apiSettings.MaxPageSize;

            _logger.LogInformation("Retrieving paged operations for DATA ENTRY - Page: {PageNumber}, Size: {PageSize}, Status: {Status}", 
                pageNumber, pageSize, status);

            bool? isActive = status.ToLower() switch
            {
                "active" => true,
                "inactive" => false,
                "all" => null,
                _ => true // Default to active
            };

            // Minimal caching for data entry to ensure fresh data
            var (items, totalCount) = await _operationService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
            
            var pagedResult = new PagedResult<OperationDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            Response.Headers.Add("X-Cache", "BYPASS");
            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page-Number", pageNumber.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(ApiResponse<PagedResult<OperationDto>>.SuccessResult(pagedResult, 
                "Operations retrieved successfully for data entry"));
        }

        /// <summary>
        /// [DATA ENTRY CLIENT] Optimized for quick form validation
        /// </summary>
        [HttpPost("entry")]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 400)]
        public async Task<ActionResult<ApiResponse<OperationDto>>> CreateForDataEntry([FromBody] CreateOperationDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest(ApiResponse<OperationDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                return BadRequest(ApiResponse<OperationDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating operation via DATA ENTRY client: {Name}", createDto.Name);

            var operation = await _operationService.CreateAsync(createDto);

            // Aggressive cache invalidation for data entry to ensure consistency
            await _cacheService.RemoveByPatternAsync("operations:");

            _logger.LogInformation("Operation created successfully via DATA ENTRY with ID: {OperationId}", operation.Id);
            
            // Return immediate response for data entry
            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Operation-Id", operation.Id.ToString());
            
            return CreatedAtAction(nameof(GetById), new { id = operation.Id }, 
                ApiResponse<OperationDto>.SuccessResult(operation, "Operation created successfully"));
        }

        /// <summary>
        /// [DATA ENTRY CLIENT] Optimized bulk operations with progress tracking
        /// </summary>
        [HttpPost("entry/bulk")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OperationDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OperationDto>>), 400)]
        public async Task<ActionResult<ApiResponse<IEnumerable<OperationDto>>>> CreateBulkForDataEntry(
            [FromBody] IEnumerable<CreateOperationDto> createDtos)
        {
            if (createDtos == null)
            {
                return BadRequest(ApiResponse<IEnumerable<OperationDto>>.ErrorResult("Request body cannot be null"));
            }

            var createDtosList = createDtos.ToList();
            
            // Smaller bulk limits for data entry client to prevent UI blocking
            var dataEntryBulkLimit = Math.Min(_apiSettings.BulkOperationLimit, 50);
            
            if (createDtosList.Count > dataEntryBulkLimit)
            {
                return BadRequest(ApiResponse<IEnumerable<OperationDto>>.ErrorResult(
                    $"Data entry bulk limit exceeded. Maximum allowed: {dataEntryBulkLimit}"));
            }

            _logger.LogInformation("Creating {Count} operations via DATA ENTRY bulk", createDtosList.Count);

            var operations = await _operationService.CreateBulkAsync(createDtosList);

            // Immediate cache invalidation
            await _cacheService.RemoveByPatternAsync("operations:");

            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Bulk-Count", operations.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<OperationDto>>.SuccessResult(operations, 
                $"{operations.Count()} operations created successfully via data entry"));
        }

        #endregion

        #region SHARED ENDPOINTS (BOTH CLIENTS)

        /// <summary>
        /// Retrieves an operation by its ID (optimized for both clients)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 404)]
        public async Task<ActionResult<ApiResponse<OperationDto>>> GetById(
            [FromRoute][Range(1, int.MaxValue)] int id,
            [FromQuery] string clientType = "shared")
        {
            _logger.LogInformation("Retrieving operation {OperationId} for {ClientType}", id, clientType);
            
            var cacheKey = CacheKeys.OperationById(id);
            var cacheExpiration = clientType == "report" 
                ? TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes * 2)
                : TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes);
                
            var cachedOperation = await _cacheService.GetAsync<OperationDto>(cacheKey);
            if (cachedOperation != null && clientType == "report")
            {
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Client-Type", clientType);
                return Ok(ApiResponse<OperationDto>.SuccessResult(cachedOperation, "Operation retrieved successfully (cached)"));
            }

            var operation = await _operationService.GetByIdAsync(id);
            if (operation == null)
            {
                return NotFound(ApiResponse<OperationDto>.ErrorResult($"Operation with ID {id} not found"));
            }

            if (clientType == "report")
            {
                await _cacheService.SetAsync(cacheKey, operation, cacheExpiration);
                Response.Headers.Add("X-Cache", "MISS");
            }
            else
            {
                Response.Headers.Add("X-Cache", "BYPASS");
            }
            
            Response.Headers.Add("X-Client-Type", clientType);
            return Ok(ApiResponse<OperationDto>.SuccessResult(operation, "Operation retrieved successfully"));
        }

        /// <summary>
        /// Updates an existing operation (optimized for data entry)
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 404)]
        public async Task<ActionResult<ApiResponse<OperationDto>>> Update(
            [FromRoute][Range(1, int.MaxValue)] int id, 
            [FromBody] UpdateOperationDto updateDto)
        {
            if (updateDto == null)
            {
                return BadRequest(ApiResponse<OperationDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                return BadRequest(ApiResponse<OperationDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Updating operation {OperationId}", id);

            var operation = await _operationService.UpdateAsync(id, updateDto);

            // Immediate cache invalidation for data consistency
            await _cacheService.RemoveByPatternAsync("operations:");
            await _cacheService.RemoveAsync(CacheKeys.OperationById(id));

            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Operation-Id", operation.Id.ToString());
            
            return Ok(ApiResponse<OperationDto>.SuccessResult(operation, "Operation updated successfully"));
        }

        /// <summary>
        /// Soft deletes an operation (optimized for data entry)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deleting operation {OperationId}", id);
            
            var result = await _operationService.SoftDeleteAsync(id);

            // Immediate cache invalidation
            await _cacheService.RemoveByPatternAsync("operations:");
            await _cacheService.RemoveAsync(CacheKeys.OperationById(id));

            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Operation-Id", id.ToString());
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Operation deleted successfully"));
        }

        /// <summary>
        /// Activates an operation
        /// </summary>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 404)]
        public async Task<ActionResult<ApiResponse<OperationDto>>> Activate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Activating operation {OperationId}", id);
            
            var operation = await _operationService.SetActiveStatusAsync(id, true);

            // Immediate cache invalidation
            await _cacheService.RemoveByPatternAsync("operations:");
            await _cacheService.RemoveAsync(CacheKeys.OperationById(id));

            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Operation-Id", operation.Id.ToString());
            
            return Ok(ApiResponse<OperationDto>.SuccessResult(operation, "Operation activated successfully"));
        }

        /// <summary>
        /// Deactivates an operation
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<OperationDto>), 404)]
        public async Task<ActionResult<ApiResponse<OperationDto>>> Deactivate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deactivating operation {OperationId}", id);
            
            var operation = await _operationService.SetActiveStatusAsync(id, false);

            // Immediate cache invalidation
            await _cacheService.RemoveByPatternAsync("operations:");
            await _cacheService.RemoveAsync(CacheKeys.OperationById(id));

            Response.Headers.Add("X-Client-Type", "entry");
            Response.Headers.Add("X-Operation-Id", operation.Id.ToString());
            
            return Ok(ApiResponse<OperationDto>.SuccessResult(operation, "Operation deactivated successfully"));
        }

        #endregion
    }
}
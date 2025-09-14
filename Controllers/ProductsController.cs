using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UretimAPI.DTOs.Common;
using UretimAPI.DTOs.Product;
using UretimAPI.Services.Interfaces;
using UretimAPI.Services.Caching;
using UretimAPI.Configuration;
using Microsoft.Extensions.Options;

namespace UretimAPI.Controllers
{
    /// <summary>
    /// Products management controller for handling manufacturing products
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICacheService _cacheService;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ICacheService cacheService,
            IOptions<ApiSettings> apiSettings,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _cacheService = cacheService;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all products with optional status filtering
        /// </summary>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>List of products based on status filter</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetAll(
            [FromQuery] string status = "active")
        {
            _logger.LogInformation("Retrieving products with status: {Status}", status);
            
            var cacheKey = $"products:all:{status}";
            
            var cachedProducts = await _cacheService.GetAsync<IEnumerable<ProductDto>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogDebug("Products retrieved from cache");
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Status-Filter", status);
                return Ok(ApiResponse<IEnumerable<ProductDto>>.SuccessResult(cachedProducts, "Products retrieved successfully (cached)"));
            }

            IEnumerable<ProductDto> products = status.ToLower() switch
            {
                "active" => await _productService.GetAllActiveAsync(),
                "inactive" => await _productService.GetInactiveAsync(),
                "all" => await _productService.GetAllAsync(),
                _ => await _productService.GetAllActiveAsync() // Default to active
            };

            await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Retrieved {Count} products with status {Status}", products.Count(), status);
            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", products.Count().ToString());
            
            return Ok(ApiResponse<IEnumerable<ProductDto>>.SuccessResult(products, "Products retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a product by its ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetById([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Retrieving product with ID: {ProductId}", id);
            
            var cacheKey = CacheKeys.ProductById(id);
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogDebug("Product {ProductId} retrieved from cache", id);
                Response.Headers.Add("X-Cache", "HIT");
                return Ok(ApiResponse<ProductDto>.SuccessResult(cachedProduct, "Product retrieved successfully (cached)"));
            }

            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                return NotFound(ApiResponse<ProductDto>.ErrorResult($"Product with ID {id} not found"));
            }

            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Product {ProductId} retrieved successfully", id);
            Response.Headers.Add("X-Cache", "MISS");
            
            return Ok(ApiResponse<ProductDto>.SuccessResult(product, "Product retrieved successfully"));
        }

        /// <summary>
        /// Retrieves a product by its product code
        /// </summary>
        /// <param name="productCode">Product code</param>
        /// <returns>Product details</returns>
        [HttpGet("code/{productCode}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetByProductCode(
            [FromRoute][Required][StringLength(50, MinimumLength = 1)] string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                _logger.LogWarning("Invalid product code parameter: empty or whitespace");
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Product code cannot be empty"));
            }

            _logger.LogInformation("Retrieving product with code: {ProductCode}", productCode);
            
            var cacheKey = CacheKeys.ProductByCode(productCode);
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogDebug("Product with code {ProductCode} retrieved from cache", productCode);
                Response.Headers.Add("X-Cache", "HIT");
                return Ok(ApiResponse<ProductDto>.SuccessResult(cachedProduct, "Product retrieved successfully (cached)"));
            }

            var product = await _productService.GetByProductCodeAsync(productCode);
            if (product == null)
            {
                _logger.LogWarning("Product with code {ProductCode} not found", productCode);
                return NotFound(ApiResponse<ProductDto>.ErrorResult($"Product with code '{productCode}' not found"));
            }

            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes));
            
            _logger.LogInformation("Product with code {ProductCode} retrieved successfully", productCode);
            Response.Headers.Add("X-Cache", "MISS");
            
            return Ok(ApiResponse<ProductDto>.SuccessResult(product, "Product retrieved successfully"));
        }

        /// <summary>
        /// Retrieves products with pagination and optional status filtering
        /// </summary>
        /// <param name="pageNumber">Page number (minimum 1)</param>
        /// <param name="pageSize">Page size (0 for default, maximum from configuration)</param>
        /// <param name="searchTerm">Search term for filtering (maximum 100 characters)</param>
        /// <param name="status">Status filter (active/inactive/all) - default: active</param>
        /// <returns>Paginated list of products</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), 400)]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged(
            [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1, 
            [FromQuery][Range(0, 1000)] int pageSize = 0, 
            [FromQuery][StringLength(100)] string? searchTerm = null,
            [FromQuery] string status = "active")
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Invalid page number: {PageNumber}", pageNumber);
                return BadRequest(ApiResponse<PagedResult<ProductDto>>.ErrorResult("Page number must be greater than 0"));
            }

            if (pageSize <= 0) pageSize = _apiSettings.DefaultPageSize;
            if (pageSize > _apiSettings.MaxPageSize) pageSize = _apiSettings.MaxPageSize;

            _logger.LogInformation("Retrieving paged products - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}, Status: {Status}", 
                pageNumber, pageSize, searchTerm ?? "none", status);

            var cacheKey = $"products:page:{pageNumber}:size:{pageSize}:search:{searchTerm ?? "all"}:status:{status}";
            var cachedResult = await _cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Paged products retrieved from cache");
                Response.Headers.Add("X-Cache", "HIT");
                Response.Headers.Add("X-Status-Filter", status);
                Response.Headers.Add("X-Total-Count", cachedResult.TotalCount.ToString());
                Response.Headers.Add("X-Page-Number", pageNumber.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", cachedResult.TotalPages.ToString());
                return Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResult(cachedResult, "Products retrieved successfully (cached)"));
            }

            bool? isActive = status.ToLower() switch
            {
                "active" => true,
                "inactive" => false,
                "all" => null,
                _ => true // Default to active
            };

            var (items, totalCount) = await _productService.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive);
            
            var pagedResult = new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            await _cacheService.SetAsync(cacheKey, pagedResult, TimeSpan.FromMinutes(_apiSettings.CacheExpirationMinutes / 2));

            _logger.LogInformation("Retrieved {ItemCount} products from page {PageNumber} (total: {TotalCount}) with status {Status}", 
                items.Count(), pageNumber, totalCount, status);

            Response.Headers.Add("X-Cache", "MISS");
            Response.Headers.Add("X-Status-Filter", status);
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page-Number", pageNumber.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResult(pagedResult, "Products retrieved successfully"));
        }

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="createDto">Product creation data</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 409)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductDto createDto)
        {
            if (createDto == null)
            {
                _logger.LogWarning("Create product called with null data");
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Product creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating new product with code: {ProductCode}, name: {Name}", 
                createDto.ProductCode, createDto.Name);

            var product = await _productService.CreateAsync(createDto);

            await _cacheService.RemoveByPatternAsync("products:");

            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
            
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, 
                ApiResponse<ProductDto>.SuccessResult(product, "Product created successfully"));
        }

        /// <summary>
        /// Creates multiple products in bulk
        /// </summary>
        /// <param name="createDtos">List of product creation data</param>
        /// <returns>Created products</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 400)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 409)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> CreateBulk([FromBody] IEnumerable<CreateProductDto> createDtos)
        {
            if (createDtos == null)
            {
                _logger.LogWarning("Bulk create products called with null data");
                return BadRequest(ApiResponse<IEnumerable<ProductDto>>.ErrorResult("Request body cannot be null"));
            }

            var createDtosList = createDtos.ToList();
            
            if (createDtosList.Count == 0)
            {
                _logger.LogWarning("Bulk create products called with empty list");
                return BadRequest(ApiResponse<IEnumerable<ProductDto>>.ErrorResult("At least one product must be provided"));
            }
            
            if (createDtosList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk operation limit exceeded: {Count} products, limit: {Limit}", 
                    createDtosList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<IEnumerable<ProductDto>>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {createDtosList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Bulk product creation validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<IEnumerable<ProductDto>>.ErrorResult("Validation failed", errors));
            }

            _logger.LogInformation("Creating {Count} products in bulk", createDtosList.Count);

            var products = await _productService.CreateBulkAsync(createDtosList);

            await _cacheService.RemoveByPatternAsync("products:");

            _logger.LogInformation("Successfully created {Count} products in bulk", products.Count());
            
            return Ok(ApiResponse<IEnumerable<ProductDto>>.SuccessResult(products, $"{products.Count()} products created successfully"));
        }

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateDto">Product update data</param>
        /// <returns>Updated product</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 409)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
            [FromRoute][Range(1, int.MaxValue)] int id, 
            [FromBody] UpdateProductDto updateDto)
        {
            if (updateDto == null)
            {
                _logger.LogWarning("Update product {ProductId} called with null data", id);
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Request body cannot be null"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()).ToList();
                _logger.LogWarning("Product {ProductId} update validation failed: {Errors}", id, string.Join(", ", errors));
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Validation failed", errors));
            }

            // Get existing product to check if ProductCode is changing
            var existingProduct = await _productService.GetByIdAsync(id);
            if (existingProduct == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update", id);
                return NotFound(ApiResponse<ProductDto>.ErrorResult($"Product with ID {id} not found"));
            }

            var oldProductCode = existingProduct.ProductCode;

            _logger.LogInformation("Updating product {ProductId} with name: {Name}, productCode: {ProductCode}", 
                id, updateDto.Name, updateDto.ProductCode);

            var product = await _productService.UpdateAsync(id, updateDto);

            // Clear cache
            await _cacheService.RemoveByPatternAsync("products:");
            await _cacheService.RemoveAsync(CacheKeys.ProductById(id));
            
            // If ProductCode changed, remove old ProductCode cache as well
            if (!string.IsNullOrEmpty(oldProductCode) && oldProductCode != updateDto.ProductCode)
            {
                await _cacheService.RemoveAsync(CacheKeys.ProductByCode(oldProductCode));
                _logger.LogInformation("Cleared cache for old ProductCode: {OldProductCode}", oldProductCode);
            }

            _logger.LogInformation("Product {ProductId} updated successfully", id);
            
            return Ok(ApiResponse<ProductDto>.SuccessResult(product, "Product updated successfully"));
        }

        /// <summary>
        /// Soft deletes a product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deleting product {ProductId}", id);
            
            var result = await _productService.SoftDeleteAsync(id);

            await _cacheService.RemoveByPatternAsync("products:");
            await _cacheService.RemoveAsync(CacheKeys.ProductById(id));

            _logger.LogInformation("Product {ProductId} deleted successfully", id);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, "Product deleted successfully"));
        }

        /// <summary>
        /// Soft deletes multiple products in bulk
        /// </summary>
        /// <param name="ids">List of product IDs</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> BulkDelete([FromBody] IEnumerable<int> ids)
        {
            if (ids == null)
            {
                _logger.LogWarning("Bulk delete products called with null data");
                return BadRequest(ApiResponse<bool>.ErrorResult("Request body cannot be null"));
            }

            var idsList = ids.ToList();
            
            if (idsList.Count == 0)
            {
                _logger.LogWarning("Bulk delete products called with empty list");
                return BadRequest(ApiResponse<bool>.ErrorResult("At least one product ID must be provided"));
            }
            
            if (idsList.Count > _apiSettings.BulkOperationLimit)
            {
                _logger.LogWarning("Bulk delete limit exceeded: {Count} products, limit: {Limit}", 
                    idsList.Count, _apiSettings.BulkOperationLimit);
                return BadRequest(ApiResponse<bool>.ErrorResult(
                    $"Bulk operation limit exceeded. Provided: {idsList.Count}, Maximum allowed: {_apiSettings.BulkOperationLimit}"));
            }

            if (idsList.Any(id => id <= 0))
            {
                _logger.LogWarning("Bulk delete contains invalid IDs: {InvalidIds}", 
                    string.Join(", ", idsList.Where(id => id <= 0)));
                return BadRequest(ApiResponse<bool>.ErrorResult("All product IDs must be positive integers"));
            }

            _logger.LogInformation("Bulk deleting {Count} products", idsList.Count);

            var result = await _productService.BulkSoftDeleteAsync(idsList);

            await _cacheService.RemoveByPatternAsync("products:");
            foreach (var id in idsList)
            {
                await _cacheService.RemoveAsync(CacheKeys.ProductById(id));
            }

            _logger.LogInformation("Successfully deleted {Count} products in bulk", idsList.Count);
            
            return Ok(ApiResponse<bool>.SuccessResult(result, $"{idsList.Count} products deleted successfully"));
        }

        /// <summary>
        /// Activates a product
        /// </summary>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Activate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Activating product {ProductId}", id);
            
            var product = await _productService.SetActiveStatusAsync(id, true);

            await _cacheService.RemoveByPatternAsync("products:");
            await _cacheService.RemoveAsync(CacheKeys.ProductById(id));

            _logger.LogInformation("Product {ProductId} activated successfully", id);
            
            return Ok(ApiResponse<ProductDto>.SuccessResult(product, "Product activated successfully"));
        }

        /// <summary>
        /// Deactivates a product
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Deactivate([FromRoute][Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Deactivating product {ProductId}", id);
            
            var product = await _productService.SetActiveStatusAsync(id, false);

            await _cacheService.RemoveByPatternAsync("products:");
            await _cacheService.RemoveAsync(CacheKeys.ProductById(id));

            _logger.LogInformation("Product {ProductId} deactivated successfully", id);
            
            return Ok(ApiResponse<ProductDto>.SuccessResult(product, "Product deactivated successfully"));
        }

        /// <summary>
        /// Gets current server time information for debugging timezone issues
        /// </summary>
        [HttpGet("debug/time")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public ActionResult<ApiResponse<object>> GetServerTime()
        {
            var timeInfo = new
            {
                UtcNow = DateTime.UtcNow,
                TurkeyTime = UretimAPI.Helpers.DateTimeHelper.Now,
                ServerLocalTime = DateTime.Now,
                TimeZoneInfo = new
                {
                    Id = UretimAPI.Helpers.DateTimeHelper.TurkeyTimeZoneInfo.Id,
                    DisplayName = UretimAPI.Helpers.DateTimeHelper.TurkeyTimeZoneInfo.DisplayName,
                    StandardName = UretimAPI.Helpers.DateTimeHelper.TurkeyTimeZoneInfo.StandardName,
                    IsDaylightSavingTime = UretimAPI.Helpers.DateTimeHelper.TurkeyTimeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow)
                }
            };

            _logger.LogInformation("Server time debug info requested");
            
            return Ok(ApiResponse<object>.SuccessResult(timeInfo, "Server time information retrieved successfully"));
        }
    }
}
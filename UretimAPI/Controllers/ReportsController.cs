using Microsoft.AspNetCore.Mvc;
using UretimAPI.DTOs.Reports;
using UretimAPI.Services.Interfaces;
using UretimAPI.DTOs.Common;

namespace UretimAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportingService reportingService, ILogger<ReportsController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
        }

        [HttpGet("production")]
        public async Task<ActionResult<ApiResponse<ProductionReportDto>>> GetProductionReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return BadRequest(ApiResponse<ProductionReportDto>.ErrorResult("startDate cannot be after endDate"));

                var report = await _reportingService.GetProductionReportAsync(startDate.Date, endDate.Date);
                return Ok(ApiResponse<ProductionReportDto>.SuccessResult(report, "Production report retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating production report");
                var errors = new List<string> { ex.Message };
                if (ex.InnerException != null) errors.Add(ex.InnerException.Message);
                return StatusCode(500, ApiResponse<ProductionReportDto>.ErrorResult("Internal server error", errors));
            }
        }

        [HttpGet("daily")]
        public async Task<ActionResult<ApiResponse<DailyReportDto>>> GetDailyReport([FromQuery] DateTime date)
        {
            try
            {
                var d = date.Date;
                var production = await _reportingService.GetProductionReportAsync(d, d);
                var productionTotals = await _reportingService.GetProductionTotalsForDateAsync(d);
                var shipments = await _reportingService.GetShipmentsForDateAsync(d);
                var shipmentTotals = await _reportingService.GetShipmentTotalsForDateAsync(d);
                var carryoverByType = await _reportingService.GetCarryoverCountsForDateAsync(d);

                var daily = new DailyReportDto
                {
                    Date = d,
                    Production = production,
                    ProductionTotals = productionTotals,
                    Shipments = shipments,
                    ShipmentTotals = shipmentTotals,
                    CarryoverCounts = carryoverByType
                };

                return Ok(ApiResponse<DailyReportDto>.SuccessResult(daily, "Daily report retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating daily report");
                var errors = new List<string> { ex.Message };
                if (ex.InnerException != null) errors.Add(ex.InnerException.Message);
                return StatusCode(500, ApiResponse<DailyReportDto>.ErrorResult("Internal server error", errors));
            }
        }

        /// <summary>
        /// Retrieves detailed information about orders with carryover (delayed orders)
        /// </summary>
        /// <param name="date">Optional filter by order creation date</param>
        /// <param name="productType">Optional filter by product type (e.g., Disk, Kampana, Poyra)</param>
        /// <param name="carryoverValue">Optional filter by carryover days (1-15, where 15 means 15+)</param>
        /// <param name="includeDetails">Include detailed information (default: true)</param>
        /// <returns>List of carryover order details</returns>
        [HttpGet("carryover-details")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CarryoverDetailsDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CarryoverDetailsDto>>), 400)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CarryoverDetailsDto>>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<CarryoverDetailsDto>>>> GetCarryoverDetails(
            [FromQuery] DateTime? date = null,
            [FromQuery] string? productType = null,
            [FromQuery] int? carryoverValue = null,
            [FromQuery] bool includeDetails = true)
        {
            try
            {
                // Validate carryoverValue parameter
                if (carryoverValue.HasValue && (carryoverValue.Value < 1 || carryoverValue.Value > 15))
                {
                    return BadRequest(ApiResponse<IEnumerable<CarryoverDetailsDto>>.ErrorResult(
                        "carryoverValue must be between 1 and 15 (where 15 represents 15 or more days)"));
                }

                // Validate productType parameter (case-insensitive check against known types)
                if (!string.IsNullOrWhiteSpace(productType))
                {
                    var validTypes = new[] { "disk", "kampana", "poyra", "drum", "hub" };
                    if (!validTypes.Contains(productType.ToLowerInvariant()))
                    {
                        _logger.LogWarning("Unknown product type filter requested: {ProductType}", productType);
                        // Don't return error, just log warning as new product types might be added
                    }
                }

                _logger.LogInformation("Retrieving carryover details with filters - date: {Date}, productType: {ProductType}, carryoverValue: {CarryoverValue}", 
                    date, productType, carryoverValue);

                var carryoverDetails = await _reportingService.GetCarryoverDetailsAsync(date, productType, carryoverValue, includeDetails);
                
                var message = $"Retrieved {carryoverDetails.Count()} carryover details";
                if (date.HasValue) message += $" for date {date.Value:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(productType)) message += $" for product type '{productType}'";
                if (carryoverValue.HasValue) message += $" with carryover value {carryoverValue.Value}";

                return Ok(ApiResponse<IEnumerable<CarryoverDetailsDto>>.SuccessResult(carryoverDetails, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving carryover details with filters - date: {Date}, productType: {ProductType}, carryoverValue: {CarryoverValue}", 
                    date, productType, carryoverValue);
                
                var errors = new List<string> { ex.Message };
                if (ex.InnerException != null) 
                    errors.Add(ex.InnerException.Message);
                
                return StatusCode(500, ApiResponse<IEnumerable<CarryoverDetailsDto>>.ErrorResult("Internal server error", errors));
            }
        }
    }
}

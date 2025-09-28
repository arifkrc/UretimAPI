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
    }
}

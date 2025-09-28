using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UretimAPI.DTOs.Reports;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Services.Interfaces;
using UretimAPI.Entities;

namespace UretimAPI.Services.Implementations
{
    public class ReportingService : IReportingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ReportingService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductionReportDto> GetProductionReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Normalize dates
                var s = startDate.Date;
                var e = endDate.Date.AddDays(1).AddTicks(-1);

                // Use DB-side aggregation to compute sums grouped by product type and product
                var query = from prod in _unitOfWork.Repository<Product>().Query()
                            where prod.IsActive
                            join ptf in _unitOfWork.ProductionTrackingForms.Query() on new { Code = prod.ProductCode, Op = prod.LastOperationId } equals new { Code = ptf.ProductCode, Op = ptf.OperationId }
                            where ptf.Date >= s && ptf.Date <= e && ptf.IsActive
                            select new { prod.Type, prod.ProductCode, prod.Name, ptf.Quantity };

                var grouped = await query
                    .GroupBy(x => new { x.Type, x.ProductCode, x.Name })
                    .Select(g => new ProductionTypeProductSum
                    {
                        Type = g.Key.Type,
                        ProductCode = g.Key.ProductCode,
                        ProductName = g.Key.Name,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToListAsync();

                // Build DTO structure
                var typeGroups = grouped
                    .GroupBy(x => x.Type)
                    .Select(g => new TypeGroupDto
                    {
                        TypeName = g.Key,
                        Products = g.Select(p => new ProductReportDto { ProductCode = p.ProductCode, ProductName = p.ProductName, Quantity = p.Quantity }).ToList(),
                        TotalQuantity = g.Sum(p => p.Quantity)
                    }).ToList();

                var total = typeGroups.Sum(t => t.TotalQuantity);

                return new ProductionReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TypeGroups = typeGroups,
                    TotalQuantity = total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating production report for range {Start} - {End}", startDate, endDate);
                throw new InvalidOperationException($"Failed to generate production report: {ex.Message}", ex);
            }
        }

        // new method to compute production totals per type (Disk/Kampana/Poyra) based on matching PTF rows to product last operation
        public async Task<ProductionTotalsDto> GetProductionTotalsForDateAsync(DateTime date)
        {
            var s = date.Date;
            var e = date.Date.AddDays(1).AddTicks(-1);

            try
            {
                // Count raw PTFs in date range for diagnostics
                var ptfCount = await _unitOfWork.ProductionTrackingForms.Query()
                    .Where(ptf => ptf.Date >= s && ptf.Date <= e && ptf.IsActive)
                    .CountAsync();

                var productCount = await _unitOfWork.Repository<Product>().Query().Where(p => p.IsActive).CountAsync();

                // Explicit join between PTF and Product to ensure DB-side translation and correct matching
                var query = from ptf in _unitOfWork.ProductionTrackingForms.Query()
                            join prod in _unitOfWork.Repository<Product>().Query()
                                on new { Code = ptf.ProductCode, Op = ptf.OperationId } equals new { Code = prod.ProductCode, Op = prod.LastOperationId }
                            where ptf.Date >= s && ptf.Date <= e && ptf.IsActive && prod.IsActive
                            select new { Type = prod.Type, ptf.Quantity, ptf.OperatorEfficiency, ptf.MachineEfficiency };

                var joinedCount = await query.CountAsync();

                _logger.LogInformation("PTF diagnostics for {Date}: ptfCount={PtfCount}, productCount={ProductCount}, joinedCount={JoinedCount}", date, ptfCount, productCount, joinedCount);

                var grouped = await query
                    .GroupBy(x => x.Type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        AvgOperator = g.Average(x => (double?)x.OperatorEfficiency),
                        AvgMachine = g.Average(x => (double?)x.MachineEfficiency)
                    })
                    .ToListAsync();

                int disk = 0, kampana = 0, poyra = 0;
                double opSum = 0, opCount = 0, mcSum = 0, mcCount = 0;

                foreach (var g in grouped)
                {
                    var key = g.Type?.ToLowerInvariant() ?? string.Empty;
                    if (key.Contains("disk")) disk += g.TotalQuantity;
                    else if (key.Contains("kampana") || key.Contains("drum")) kampana += g.TotalQuantity;
                    else if (key.Contains("poyra") || key.Contains("hub")) poyra += g.TotalQuantity;
                    else
                    {
                        _logger.LogDebug("Unknown product type encountered in production totals: {Type}", g.Type);
                    }

                    if (g.AvgOperator.HasValue)
                    {
                        opSum += g.AvgOperator.Value * g.TotalQuantity; // weight by quantity
                        opCount += g.TotalQuantity;
                    }
                    if (g.AvgMachine.HasValue)
                    {
                        mcSum += g.AvgMachine.Value * g.TotalQuantity;
                        mcCount += g.TotalQuantity;
                    }
                }

                var combined = disk + kampana + poyra;

                var avgOp = opCount > 0 ? opSum / opCount : 0.0;
                var avgMc = mcCount > 0 ? mcSum / mcCount : 0.0;

                _logger.LogDebug("Production totals for {Date}: disk={Disk} kampana={Kampana} poyra={Poyra} combined={Combined} avgOp={AvgOp} avgMc={AvgMc}", date, disk, kampana, poyra, combined, avgOp, avgMc);
                return new ProductionTotalsDto { DiskTotal = disk, KampanaTotal = kampana, PoyraTotal = poyra, CombinedTotal = combined, AverageOperatorEfficiency = avgOp, AverageMachineEfficiency = avgMc };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute production totals for date {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<UretimAPI.DTOs.Shipment.ShipmentDto>> GetShipmentsForDateAsync(DateTime date)
        {
            try
            {
                var start = date.Date;
                var end = date.Date.AddDays(1).AddTicks(-1);
                var shipments = await _unitOfWork.Shipments.GetByDateRangeAsync(start, end);
                return _mapper.Map<IEnumerable<UretimAPI.DTOs.Shipment.ShipmentDto>>(shipments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching shipments for date {Date}", date);
                throw new InvalidOperationException($"Failed to get shipments: {ex.Message}", ex);
            }
        }

        public async Task<UretimAPI.DTOs.Reports.ShipmentTotalsDto> GetShipmentTotalsForDateAsync(DateTime date)
        {
            var start = date.Date;
            var end = date.Date.AddDays(1).AddTicks(-1);
            var shipments = (await _unitOfWork.Shipments.GetByDateRangeAsync(start, end)).ToList();

            var domestic = shipments.Where(s => s.Domestic).ToList();
            var abroad = shipments.Where(s => s.Abroad).ToList();

            UretimAPI.DTOs.Reports.ShipmentTypeTotals Totals(IEnumerable<Shipment> list) => new UretimAPI.DTOs.Reports.ShipmentTypeTotals
            {
                DiskTotal = list.Sum(s => s.Disk ?? 0),
                KampanaTotal = list.Sum(s => s.Kampana ?? 0),
                PoyraTotal = list.Sum(s => s.Poyra ?? 0),
                CombinedTotal = list.Sum(s => (s.Disk ?? 0) + (s.Kampana ?? 0) + (s.Poyra ?? 0))
            };

            return new UretimAPI.DTOs.Reports.ShipmentTotalsDto
            {
                Domestic = Totals(domestic),
                Abroad = Totals(abroad)
            };
        }

        public async Task<int> GetTotalProducedAsync(DateTime startDate, DateTime endDate)
        {
            var s = startDate.Date;
            var e = endDate.Date.AddDays(1).AddTicks(-1);

            var products = (await _unitOfWork.Products.GetAllActiveAsync()).ToList();
            var ptfs = (await _unitOfWork.ProductionTrackingForms.GetByDateRangeAsync(s, e)).ToList();

            int total = 0;

            foreach (var prod in products)
            {
                if (prod.LastOperationId <= 0) continue;
                var qty = ptfs.Where(p => p.OperationId == prod.LastOperationId && p.ProductCode == prod.ProductCode).Sum(p => p.Quantity);
                total += qty;
            }

            return total;
        }

        public async Task<IEnumerable<CarryoverByTypeDto>> GetCarryoverCountsForDateAsync(DateTime date)
        {
            // For carryover counts we do not filter by date; include all active orders
            var orders = (await _unitOfWork.Orders.GetAllActiveAsync()).ToList();

            if (!orders.Any()) return new List<CarryoverByTypeDto>();

            // Load products for mapping productCode -> Type
            var productCodes = orders.Select(o => o.ProductCode).Distinct().ToList();
            var products = (await _unitOfWork.Products.FindAsync(p => productCodes.Contains(p.ProductCode))).ToList();
            var prodMap = products.ToDictionary(p => p.ProductCode, p => p.Type, StringComparer.OrdinalIgnoreCase);

            // Build per-type buckets 1..15
            var buckets = Enumerable.Range(1, 15).ToList();

            var groupedByType = orders
                .Select(o => new { o.ProductCode, Carry = Math.Max(0, o.Carryover) })
                .Where(x => x.Carry > 0)
                .Select(x => new { Type = prodMap.ContainsKey(x.ProductCode) ? prodMap[x.ProductCode] : "Unknown", Carry = x.Carry >= 15 ? 15 : x.Carry })
                .GroupBy(x => x.Type)
                .ToList();

            var result = new List<CarryoverByTypeDto>();

            foreach (var g in groupedByType)
            {
                var counts = g.GroupBy(x => x.Carry).ToDictionary(k => k.Key, v => v.Count());
                var bucketDtos = buckets.Select(b => new CarryoverCountDto { CarryoverValue = b, Count = counts.ContainsKey(b) ? counts[b] : 0 }).ToList();
                result.Add(new CarryoverByTypeDto { ProductType = g.Key, Buckets = bucketDtos });
            }

            return result;
        }
    }
}

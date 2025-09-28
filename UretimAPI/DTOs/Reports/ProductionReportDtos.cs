namespace UretimAPI.DTOs.Reports
{
    public class ProductionReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IEnumerable<TypeGroupDto> TypeGroups { get; set; } = new List<TypeGroupDto>();
        public int TotalQuantity { get; set; }
    }

    public class TypeGroupDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public IEnumerable<ProductReportDto> Products { get; set; } = new List<ProductReportDto>();
    }

    public class ProductReportDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ShipmentTypeTotals
    {
        public int DiskTotal { get; set; }
        public int KampanaTotal { get; set; }
        public int PoyraTotal { get; set; }
        public int CombinedTotal { get; set; }
    }

    public class ShipmentTotalsDto
    {
        // Totals split by domestic/abroad
        public ShipmentTypeTotals Domestic { get; set; } = new ShipmentTypeTotals();
        public ShipmentTypeTotals Abroad { get; set; } = new ShipmentTypeTotals();
        // Convenience combined totals (sum of domestic+abroad)
        public ShipmentTypeTotals Combined => new ShipmentTypeTotals
        {
            DiskTotal = Domestic.DiskTotal + Abroad.DiskTotal,
            KampanaTotal = Domestic.KampanaTotal + Abroad.KampanaTotal,
            PoyraTotal = Domestic.PoyraTotal + Abroad.PoyraTotal,
            CombinedTotal = Domestic.CombinedTotal + Abroad.CombinedTotal
        };
    }

    public class ProductionTotalsDto
    {
        public int DiskTotal { get; set; }
        public int KampanaTotal { get; set; }
        public int PoyraTotal { get; set; }
        public int CombinedTotal { get; set; }

        // average efficiencies (percent or fraction depending on stored values)
        public double AverageOperatorEfficiency { get; set; }
        public double AverageMachineEfficiency { get; set; }
    }

    public class CarryoverCountDto
    {
        // CarryoverValue: 1..14, 15 means 15 or more
        public int CarryoverValue { get; set; }
        public int Count { get; set; }
    }

    public class CarryoverByTypeDto
    {
        public string ProductType { get; set; } = string.Empty;
        // Buckets 1..15 where 15 means 15 or more
        public IEnumerable<CarryoverCountDto> Buckets { get; set; } = new List<CarryoverCountDto>();
    }

    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public ProductionReportDto Production { get; set; } = new ProductionReportDto();
        public ProductionTotalsDto ProductionTotals { get; set; } = new ProductionTotalsDto();
        public IEnumerable<UretimAPI.DTOs.Shipment.ShipmentDto> Shipments { get; set; } = new List<UretimAPI.DTOs.Shipment.ShipmentDto>();
        public ShipmentTotalsDto ShipmentTotals { get; set; } = new ShipmentTotalsDto();
        public IEnumerable<CarryoverByTypeDto> CarryoverCounts { get; set; } = new List<CarryoverByTypeDto>();
    }
}

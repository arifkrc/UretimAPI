namespace UretimAPI.DTOs.Reports
{
    // DTO returned from DB-level aggregation
    public class ProductionTypeProductSum
    {
        public string Type { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}

namespace UretimAPI.Entities
{
    public class ProductionTrackingForm
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string? Line { get; set; }
        public string? ShiftSupervisor { get; set; }
        public string? Machine { get; set; }
        public string? OperatorName { get; set; }
        public string? SectionSupervisor { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Operation { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        // Defect and downtime fields - nullable
        public int? CastingDefect { get; set; }
        public int? ProcessingDefect { get; set; }
        public int? MachineFailure { get; set; }
        public int? SettingMachine { get; set; }
        public int? DiamondChange { get; set; }
        public int? RawWaiting { get; set; }
        public int? Cleaning { get; set; }
        
        // Navigation property - Product null olamaz
        public Product Product { get; set; } = null!;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
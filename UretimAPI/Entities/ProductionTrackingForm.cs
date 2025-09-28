using UretimAPI.Helpers;

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
        
        // Replace free-text Operation with relation to Operation entity
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;

        // Store time-only values as TimeSpan? (HH:mm)
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        
        // Defect and downtime fields - nullable
        public int? CastingDefect { get; set; }
        public int? ProcessingDefect { get; set; }
        public int? MachineFailure { get; set; }
        public int? SettingMachine { get; set; }
        public int? DiamondChange { get; set; }
        public int? RawWaiting { get; set; }
        public int? Cleaning { get; set; }
        public int? CycleTime { get; set; }

        // New: efficiencies (nullable double)
        public double? OperatorEfficiency { get; set; }
        public double? MachineEfficiency { get; set; }
        
        // Navigation property - Product null olamaz
        public Product Product { get; set; } = null!;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTimeHelper.Now;
        public bool IsActive { get; set; } = true;
    }
}
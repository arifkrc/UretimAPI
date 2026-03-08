using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.ProductionTrackingForm
{
    public class ProductionTrackingFormDto
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
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int OperationId { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string OperationShortCode { get; set; } = string.Empty;
        public int? CastingDefect { get; set; }
        public int? ProcessingDefect { get; set; }
        public int? MachineFailure { get; set; }
        public int? SettingMachine { get; set; }
        public int? DiamondChange { get; set; }
        public int? RawWaiting { get; set; }
        public int? Cleaning { get; set; }
        public decimal? CycleTime { get; set; }
        public decimal? MachineEfficiency { get; set; }
        public decimal? OperatorEfficiency { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateProductionTrackingFormDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [RequiredNotEmpty]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Shift must be between 1 and 50 characters.")]
        public string Shift { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Line cannot exceed 100 characters.")]
        public string? Line { get; set; }

        [StringLength(100, ErrorMessage = "Shift supervisor name cannot exceed 100 characters.")]
        public string? ShiftSupervisor { get; set; }

        [StringLength(100, ErrorMessage = "Machine name cannot exceed 100 characters.")]
        public string? Machine { get; set; }

        [StringLength(100, ErrorMessage = "Operator name cannot exceed 100 characters.")]
        public string? OperatorName { get; set; }

        [StringLength(100, ErrorMessage = "Section supervisor name cannot exceed 100 characters.")]
        public string? SectionSupervisor { get; set; }

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
        public int Quantity { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "OperationId must be a positive integer.")]
        public int OperationId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Casting defect count cannot be negative.")]
        public int? CastingDefect { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Processing defect count cannot be negative.")]
        public int? ProcessingDefect { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Machine failure count cannot be negative.")]
        public int? MachineFailure { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Setting machine count cannot be negative.")]
        public int? SettingMachine { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Diamond change count cannot be negative.")]
        public int? DiamondChange { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Raw waiting count cannot be negative.")]
        public int? RawWaiting { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Cleaning count cannot be negative.")]
        public int? Cleaning { get; set; }

        public int? CycleTime { get; set; }
        public double? MachineEfficiency { get; set; }
        public double? OperatorEfficiency { get; set; }
    }

    public class UpdateProductionTrackingFormDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [RequiredNotEmpty]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Shift must be between 1 and 50 characters.")]
        public string Shift { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Line cannot exceed 100 characters.")]
        public string? Line { get; set; }

        [StringLength(100, ErrorMessage = "Shift supervisor name cannot exceed 100 characters.")]
        public string? ShiftSupervisor { get; set; }

        [StringLength(100, ErrorMessage = "Machine name cannot exceed 100 characters.")]
        public string? Machine { get; set; }

        [StringLength(100, ErrorMessage = "Operator name cannot exceed 100 characters.")]
        public string? OperatorName { get; set; }

        [StringLength(100, ErrorMessage = "Section supervisor name cannot exceed 100 characters.")]
        public string? SectionSupervisor { get; set; }

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
        public int Quantity { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "OperationId must be a positive integer.")]
        public int OperationId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Casting defect count cannot be negative.")]
        public int? CastingDefect { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Processing defect count cannot be negative.")]
        public int? ProcessingDefect { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Machine failure count cannot be negative.")]
        public int? MachineFailure { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Setting machine count cannot be negative.")]
        public int? SettingMachine { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Diamond change count cannot be negative.")]
        public int? DiamondChange { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Raw waiting count cannot be negative.")]
        public int? RawWaiting { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Cleaning count cannot be negative.")]
        public int? Cleaning { get; set; }

        public int? CycleTime { get; set; }
        public double? MachineEfficiency { get; set; }
        public double? OperatorEfficiency { get; set; }
    }

    public class BulkCreateProductionTrackingFormDto
    {
        public List<CreateProductionTrackingFormDto> ProductionTrackingForms { get; set; } = new();
    }
}
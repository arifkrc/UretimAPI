using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.CycleTime
{
    public class CycleTimeDto
    {
        public int Id { get; set; }
        public int OperationId { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string OperationShortCode { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Second { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCycleTimeDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "OperationId must be a positive integer.")]
        public int OperationId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")]
        public int ProductId { get; set; }

        [Range(1, 86400, ErrorMessage = "Second must be between 1 and 86400 (24 hours).")]
        public int Second { get; set; }
    }

    public class UpdateCycleTimeDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "OperationId must be a positive integer.")]
        public int OperationId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")]
        public int ProductId { get; set; }

        [Range(1, 86400, ErrorMessage = "Second must be between 1 and 86400 (24 hours).")]
        public int Second { get; set; }
    }

    public class BulkCreateCycleTimeDto
    {
        public List<CreateCycleTimeDto> CycleTimes { get; set; } = new();
    }
}
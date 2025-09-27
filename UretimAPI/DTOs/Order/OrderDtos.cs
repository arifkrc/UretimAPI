using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderAddedDateTime { get; set; } = string.Empty;
        public string DocumentNo { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Variants { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int Carryover { get; set; }
        public int CompletedQuantity { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrderDto
    {
        [RequiredNotEmpty]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Order added date time must be between 1 and 20 characters.")]
        public string OrderAddedDateTime { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Document number must be between 1 and 100 characters.")]
        public string DocumentNo { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer name must be between 1 and 200 characters.")]
        public string Customer { get; set; } = string.Empty;

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        // Variants allow large values (nvarchar(max) in DB)
        public string Variants { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Order count must be a positive integer.")]
        public int OrderCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Carryover cannot be negative.")]
        public int Carryover { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Completed quantity cannot be negative.")]
        public int CompletedQuantity { get; set; } = 0;
    }

    public class UpdateOrderDto
    {
        [RequiredNotEmpty]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Order added date time must be between 1 and 20 characters.")]
        public string OrderAddedDateTime { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Customer name must be between 1 and 200 characters.")]
        public string Customer { get; set; } = string.Empty;

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        // Variants allow large values (nvarchar(max) in DB)
        public string Variants { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Order count must be a positive integer.")]
        public int OrderCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Carryover cannot be negative.")]
        public int Carryover { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Completed quantity cannot be negative.")]
        public int CompletedQuantity { get; set; }
    }

    public class BulkCreateOrderDto
    {
        public List<CreateOrderDto> Orders { get; set; } = new();
    }
}
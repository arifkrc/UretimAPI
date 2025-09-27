using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.Packing
{
    public class PackingDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Shift { get; set; }
        public string? Supervisor { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ExplodedFrom { get; set; }
        public string? ExplodingTo { get; set; }
        public int? RelatedWithOrder { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreatePackingDto
    {
        public DateTime? Date { get; set; }

        [StringLength(50, ErrorMessage = "Shift cannot exceed 50 characters.")]
        public string? Shift { get; set; }

        [StringLength(100, ErrorMessage = "Supervisor name cannot exceed 100 characters.")]
        public string? Supervisor { get; set; }

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
        public int Quantity { get; set; }

        [StringLength(100, ErrorMessage = "Exploded from cannot exceed 100 characters.")]
        public string? ExplodedFrom { get; set; }

        [StringLength(100, ErrorMessage = "Exploding to cannot exceed 100 characters.")]
        public string? ExplodingTo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "RelatedWithOrder must be a positive integer when provided.")]
        public int? RelatedWithOrder { get; set; }
    }

    public class UpdatePackingDto
    {
        public DateTime? Date { get; set; }

        [StringLength(50, ErrorMessage = "Shift cannot exceed 50 characters.")]
        public string? Shift { get; set; }

        [StringLength(100, ErrorMessage = "Supervisor name cannot exceed 100 characters.")]
        public string? Supervisor { get; set; }

        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
        public int Quantity { get; set; }

        [StringLength(100, ErrorMessage = "Exploded from cannot exceed 100 characters.")]
        public string? ExplodedFrom { get; set; }

        [StringLength(100, ErrorMessage = "Exploding to cannot exceed 100 characters.")]
        public string? ExplodingTo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "RelatedWithOrder must be a positive integer when provided.")]
        public int? RelatedWithOrder { get; set; }
    }

    public class BulkCreatePackingDto
    {
        public List<CreatePackingDto> Packings { get; set; } = new();
    }
}
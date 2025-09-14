using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.Product
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int LastOperationId { get; set; }
        public string LastOperationName { get; set; } = string.Empty;
        public string LastOperationShortCode { get; set; } = string.Empty;
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateProductDto
    {
        [ProductCode]
        public string ProductCode { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Type must be between 2 and 100 characters.")]
        public string Type { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "LastOperationId must be a positive integer.")]
        public int LastOperationId { get; set; }
    }

    public class UpdateProductDto
    {
        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [RequiredNotEmpty]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Type must be between 2 and 100 characters.")]
        public string Type { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "LastOperationId must be a positive integer.")]
        public int LastOperationId { get; set; }
    }
}
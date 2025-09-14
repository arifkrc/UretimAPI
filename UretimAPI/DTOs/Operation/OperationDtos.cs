using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.Operation
{
    public class OperationDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "ShortCode is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "ShortCode must be between 1 and 20 characters")]
        public string ShortCode { get; set; } = string.Empty;

        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOperationDto
    {
        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [ShortCode]
        [Required(ErrorMessage = "ShortCode is required")]
        public string ShortCode { get; set; } = string.Empty;
    }

    public class UpdateOperationDto
    {
        [RequiredNotEmpty]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [ShortCode]
        [Required(ErrorMessage = "ShortCode is required")]
        public string ShortCode { get; set; } = string.Empty;
    }
}
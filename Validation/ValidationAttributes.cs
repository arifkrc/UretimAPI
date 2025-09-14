using System.ComponentModel.DataAnnotations;

namespace UretimAPI.Validation
{
    public class RequiredNotEmptyAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value switch
            {
                null => false,
                string str => !string.IsNullOrWhiteSpace(str),
                _ => true
            };
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field is required and cannot be empty or whitespace.";
        }
    }

    public class ProductCodeAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string productCode)
                return false;

            // Product code validation rules
            return !string.IsNullOrWhiteSpace(productCode) 
                   && productCode.Length >= 2 
                   && productCode.Length <= 50
                   && productCode.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} must be 2-50 characters long and contain only letters, numbers, hyphens, or underscores.";
        }
    }

    public class ShortCodeAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string shortCode)
                return false;

            return !string.IsNullOrWhiteSpace(shortCode) 
                   && shortCode.Length >= 1 
                   && shortCode.Length <= 20
                   && shortCode.All(char.IsLetterOrDigit);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} must be 1-20 characters long and contain only letters and numbers.";
        }
    }
}
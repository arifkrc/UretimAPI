namespace UretimAPI.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
        public BusinessException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        public ValidationException(string message, List<string> errors) : base(message)
        {
            Errors = errors ?? new List<string>();
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key) 
            : base($"{entityName} with key '{key}' was not found.") { }
    }

    public class DuplicateException : Exception
    {
        public DuplicateException(string message) : base(message) { }
        public DuplicateException(string entityName, string field, object value)
            : base($"{GetTurkishEntityName(entityName)} {GetTurkishFieldName(field)} '{value}' zaten mevcut. Lütfen farkl? bir {GetTurkishFieldName(field)} girin.") { }

        private static string GetTurkishEntityName(string entityName)
        {
            return entityName.ToLower() switch
            {
                "product" => "Ürün",
                "operation" => "Operasyon",
                "order" => "Sipari?",
                "cycletime" => "Çevrim Süresi",
                _ => entityName
            };
        }

        private static string GetTurkishFieldName(string fieldName)
        {
            return fieldName.ToLower() switch
            {
                "productcode" => "ürün kodu",
                "shortcode" => "k?sa kod",
                "documentno" => "belge numaras?",
                "productid-operationid" => "ürün-operasyon kombinasyonu",
                _ => fieldName
            };
        }
    }
}
using UretimAPI.Helpers;

namespace UretimAPI.Entities
{
    public class Packing
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Shift { get; set; }
        public string? Supervisor { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ExplodedFrom { get; set; }
        public string? ExplodingTo { get; set; }
        
        // Navigation property - Product null olamaz
        public Product Product { get; set; } = null!;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTimeHelper.Now;
        public bool IsActive { get; set; } = true;
    }
}
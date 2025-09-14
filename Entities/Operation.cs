using UretimAPI.Helpers;

namespace UretimAPI.Entities
{
    public class Operation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTimeHelper.Now;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<Product> ProductsWithLastOperation { get; set; } = new List<Product>();
        public ICollection<CycleTime> CycleTimes { get; set; } = new List<CycleTime>();
    }
}
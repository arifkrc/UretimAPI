using UretimAPI.Helpers;

namespace UretimAPI.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderAddedDateTime { get; set; } = string.Empty;
        public string DocumentNo { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Variants { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int Carryover { get; set; }
        public int CompletedQuantity { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        // Common field (standard AddedDateTime)
        public DateTime AddedDateTime { get; set; } = DateTimeHelper.Now;
    }
}
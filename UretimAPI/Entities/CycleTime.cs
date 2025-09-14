namespace UretimAPI.Entities
{
    public class CycleTime
    {
        public int Id { get; set; }
        public int OperationId { get; set; }
        public int ProductId { get; set; }
        public int Second { get; set; }
        
        // Navigation properties
        public Operation Operation { get; set; } = null!;
        public Product Product { get; set; } = null!;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
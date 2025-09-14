namespace UretimAPI.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int LastOperationId { get; set; }
        
        // Navigation property
        public Operation LastOperation { get; set; } = null!;
        
        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties for relationships
        public ICollection<CycleTime> CycleTimes { get; set; } = new List<CycleTime>();
        public ICollection<ProductionTrackingForm> ProductionTrackingForms { get; set; } = new List<ProductionTrackingForm>();
        public ICollection<Packing> Packings { get; set; } = new List<Packing>();
    }
}
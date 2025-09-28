using UretimAPI.Helpers;

namespace UretimAPI.Entities
{
    public class Shipment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        // counts/measurements
        public int? Disk { get; set; }
        public int? Kampana { get; set; }
        public int? Poyra { get; set; }

        // flags
        public bool Abroad { get; set; }
        public bool Domestic { get; set; }

        // Common fields
        public DateTime AddedDateTime { get; set; } = DateTimeHelper.Now;
        public bool IsActive { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations;
using UretimAPI.Validation;

namespace UretimAPI.DTOs.Shipment
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int? Disk { get; set; }
        public int? Kampana { get; set; }
        public int? Poyra { get; set; }
        public bool Abroad { get; set; }
        public bool Domestic { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateShipmentDto
    {
        [Required]
        public DateTime Date { get; set; }
        public int? Disk { get; set; }
        public int? Kampana { get; set; }
        public int? Poyra { get; set; }
        public bool Abroad { get; set; }
        public bool Domestic { get; set; }
    }

    public class UpdateShipmentDto
    {
        [Required]
        public DateTime Date { get; set; }
        public int? Disk { get; set; }
        public int? Kampana { get; set; }
        public int? Poyra { get; set; }
        public bool Abroad { get; set; }
        public bool Domestic { get; set; }
    }

    public class BulkCreateShipmentDto
    {
        public List<CreateShipmentDto> Shipments { get; set; } = new();
    }
}

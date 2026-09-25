using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    [Index(nameof(RoomId))]
    public record Device
    {
        [Key]
        [MaxLength(12)]
        public required string MacAddress { get; set; }

        public Guid? RoomId { get; set; }

        [MaxLength(128)]
        public required string Name { get; set; }

        public required DeviceType DeviceType { get; set; }
    }
}

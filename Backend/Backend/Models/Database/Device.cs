using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// A board paired into a room. The MAC is the key, so a device belongs to
    /// exactly one room and its telemetry has one owner.
    /// </summary>
    [Index(nameof(RoomId))]
    public record Device
    {
        /// <summary>
        /// Stored normalized: upper case, no separators.
        /// </summary>
        [Key]
        [MaxLength(12)]
        public required string MacAddress { get; set; }

        public required Guid RoomId { get; set; }

        [MaxLength(128)]
        public required string Name { get; set; }
    }
}

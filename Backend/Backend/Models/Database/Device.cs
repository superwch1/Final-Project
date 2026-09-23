using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
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

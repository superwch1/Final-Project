using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public record UpdateRoomRequest
    {
        [MinLength(1)]
        [MaxLength(128)]
        public required string Name { get; init; }
    }
}

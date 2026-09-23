using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public record PairDeviceRequest
    {
        [MinLength(12)]
        [MaxLength(32)]
        public required string MacAddress { get; init; }

        [MinLength(1)]
        [MaxLength(128)]
        public required string Name { get; init; }
    }
}

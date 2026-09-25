using Backend.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public record CreatePolicyRequest
    {
        [MinLength(12)]
        [MaxLength(32)]
        public required string SensorMacAddress { get; init; }

        [MinLength(12)]
        [MaxLength(32)]
        public required string ActuatorMacAddress { get; init; }

        public required SensorReading Reading { get; init; }

        public required Comparison Comparison { get; init; }

        public required double Threshold { get; init; }

        public required ActuatorState ActuatorState { get; init; }
    }
}

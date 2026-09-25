using Backend.Enumerations;

namespace Backend.Models
{
    public record UpdatePolicyRequest
    {
        public required SensorReading Reading { get; init; }

        public required Comparison Comparison { get; init; }

        public required double Threshold { get; init; }

        public required ActuatorState ActuatorState { get; init; }

        public required bool IsEnabled { get; init; }
    }
}

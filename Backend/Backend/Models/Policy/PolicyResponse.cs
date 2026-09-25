using Backend.Enumerations;

namespace Backend.Models
{
    public record PolicyResponse
    {
        public required Guid Id { get; init; }

        public required string SensorMacAddress { get; init; }

        public required string SensorName { get; init; }

        public required string ActuatorMacAddress { get; init; }

        public required string ActuatorName { get; init; }

        public required SensorReading Reading { get; init; }

        public required Comparison Comparison { get; init; }

        public required double Threshold { get; init; }

        public required ActuatorState ActuatorState { get; init; }

        public required bool IsEnabled { get; init; }

        public static PolicyResponse FromPolicy(Policy policy)
        {
            return new PolicyResponse
            {
                Id = policy.Id,
                SensorMacAddress = policy.SensorMacAddress,
                SensorName = policy.Sensor?.Name ?? string.Empty,
                ActuatorMacAddress = policy.ActuatorMacAddress,
                ActuatorName = policy.Actuator?.Name ?? string.Empty,
                Reading = policy.Reading,
                Comparison = policy.Comparison,
                Threshold = policy.Threshold,
                ActuatorState = policy.ActuatorState,
                IsEnabled = policy.IsEnabled
            };
        }
    }
}

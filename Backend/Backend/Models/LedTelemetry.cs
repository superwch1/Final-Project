using Backend.Enumerations;

namespace Backend.Models
{
    public record LedTelemetry : ITelemetry
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

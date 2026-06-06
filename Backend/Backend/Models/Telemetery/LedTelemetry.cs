using Backend.Enumerations;

namespace Backend.Models
{
    public record LedTelemetry : BaseTelemetry
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

using Backend.Enumerations;

namespace Backend.Models
{
    public record LedActuatorTelemetry : BaseTelemetry
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

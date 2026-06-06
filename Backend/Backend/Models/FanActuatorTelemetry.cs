using Backend.Enumerations;

namespace Backend.Models
{
    public record FanActuatorTelemetry : BaseTelemetry
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

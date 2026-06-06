using Backend.Enumerations;

namespace Backend.Models
{
    public record FanTelemetry : BaseTelemetry
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

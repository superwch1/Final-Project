using Backend.Enumerations;

namespace Backend.Models
{
    public record LightTelemetry : BaseTelemetry
    {
        public int LightReading { get; init; }
    }
}

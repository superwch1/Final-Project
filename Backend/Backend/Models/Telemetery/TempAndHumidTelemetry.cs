namespace Backend.Models
{
    public record TempAndHumidTelemetry : BaseTelemetry
    {
        public float TemperatureReading { get; init; }

        public float HumidityReading { get; init; }
    }
}

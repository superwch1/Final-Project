namespace Backend.Models
{
    public record TempAndHumidTelemetry : BaseTelemetry
    {
        public int TemperatureReading { get; init; }

        public int HumidityReading { get; init; }
    }
}

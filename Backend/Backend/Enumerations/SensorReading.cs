using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// The type of reading a sensor reports.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SensorReading
    {
        Temperature,
        Humidity,
        Light
    }
}

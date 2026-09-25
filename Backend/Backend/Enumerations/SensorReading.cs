using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// Which value a policy watches
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SensorReading
    {
        Temperature,
        Humidity,
        Light
    }
}

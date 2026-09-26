using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// How a policy compares a sensor reading against its threshold.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Comparison
    {
        Above,
        Below
    }
}

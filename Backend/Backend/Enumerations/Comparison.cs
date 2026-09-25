using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// How a reading is compared against a policy's threshold.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Comparison
    {
        Above,
        Below
    }
}

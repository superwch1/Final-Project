using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// The on or off state of an actuator.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActuatorState
    {
        Off,
        On
    }
}

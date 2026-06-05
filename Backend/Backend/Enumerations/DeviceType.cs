using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeviceType
    {
        Unknown,
        LightSensor,
        TempSensor,
        LedActuator,
        FanActuator,
    }

    public static class DeviceTypeExtensions
    {
        public static bool IsActuator(this DeviceType deviceType)
        {
            if (deviceType == DeviceType.LedActuator || deviceType == DeviceType.FanActuator)
                return true;

            return false;
        }
    }
}

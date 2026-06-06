using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeviceType
    {
        Unknown,
        LightSensor,
        TempAndHumidSensor,
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

        public static bool IsSensor(this DeviceType deviceType)
        {
            if (deviceType == DeviceType.LightSensor || deviceType == DeviceType.TempAndHumidSensor)
                return true;

            return false;
        }
    }
}

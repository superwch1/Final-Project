using System.Text.Json.Serialization;

namespace Backend.Enumerations
{
    /// <summary>
    /// The type of device connected to the backend.
    /// </summary>
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
        /// <summary>
        /// Returns true if the device type is an actuator.
        /// </summary>
        public static bool IsActuator(this DeviceType deviceType)
        {
            if (deviceType == DeviceType.LedActuator || deviceType == DeviceType.FanActuator)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the device type is a sensor.
        /// </summary>
        public static bool IsSensor(this DeviceType deviceType)
        {
            if (deviceType == DeviceType.LightSensor || deviceType == DeviceType.TempAndHumidSensor)
                return true;

            return false;
        }
    }
}

using Backend.Enumerations;
using Backend.Models;

namespace Backend.Tests.Helpers
{
    public static class TestData
    {
        public const string SensorMacAddress = "AABBCCDDEE01";
        public const string ActuatorMacAddress = "AABBCCDDEE02";

        /// <summary>
        /// Builds an account with a normalised email
        /// </summary>
        public static Account Account(string email = "USER@EXAMPLE.COM")
        {
            return new Account
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = "hash",
                Name = "User",
                FailedAttemptCount = 0
            };
        }

        /// <summary>
        /// Builds a room 
        /// </summary>
        public static Room Room(Guid accountId, string name = "Living room")
        {
            return new Room
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Name = name
            };
        }

        /// <summary>
        /// Build a device and pair to the room
        /// </summary>
        public static Device Device(string macAddress, DeviceType deviceType, Guid? roomId = null)
        {
            return new Device
            {
                MacAddress = macAddress,
                RoomId = roomId,
                Name = deviceType.ToString(),
                DeviceType = deviceType
            };
        }

        /// <summary>
        /// Build a policy
        /// </summary>
        public static Policy Policy(string sensorMacAddress = SensorMacAddress, string actuatorMacAddress = ActuatorMacAddress)
        {
            return new Policy
            {
                Id = Guid.NewGuid(),
                SensorMacAddress = sensorMacAddress,
                ActuatorMacAddress = actuatorMacAddress,
                Reading = SensorReading.Light,
                Comparison = Comparison.Above,
                Threshold = 50,
                ActuatorState = ActuatorState.On,
                IsEnabled = true
            };
        }
    }
}

using Backend.Enumerations;

namespace Backend.Models
{
    public record DeviceResponse
    {
        public required string MacAddress { get; init; }

        public required string Name { get; init; }

        public required DeviceType DeviceType { get; init; }

        public static DeviceResponse FromDevice(Device device)
        {
            return new DeviceResponse
            {
                MacAddress = device.MacAddress,
                Name = device.Name,
                DeviceType = device.DeviceType
            };
        }
    }
}

namespace Backend.Models
{
    public record DeviceResponse
    {
        public required string MacAddress { get; init; }

        public required string Name { get; init; }

        public static DeviceResponse FromDevice(Device device)
        {
            return new DeviceResponse
            {
                MacAddress = device.MacAddress,
                Name = device.Name
            };
        }
    }
}

namespace Backend.Models
{
    /// <summary>
    /// A room and the devices paired into it.
    /// </summary>
    public record RoomResponse
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required IEnumerable<DeviceResponse> Devices { get; init; }

        public static RoomResponse FromRoom(Room room)
        {
            return new RoomResponse
            {
                Id = room.Id,
                Name = room.Name,
                Devices = room.Devices.Select(DeviceResponse.FromDevice).ToList()
            };
        }
    }
}

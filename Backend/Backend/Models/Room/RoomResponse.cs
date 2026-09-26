namespace Backend.Models
{
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

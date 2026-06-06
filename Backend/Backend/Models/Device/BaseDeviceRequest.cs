namespace Backend.Models
{
    public abstract record BaseDeviceRequest
    {
        public required string MacAddress { get; init; }
    }
}

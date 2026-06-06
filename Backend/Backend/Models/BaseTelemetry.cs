using Backend.Enumerations;

namespace Backend.Models
{
    public abstract record BaseTelemetry
    {
        public DeviceType DeviceType { get; init; }

        public int WiFiSignal { get; init; }

        public int FreeHeap { get; init; }
    }
}

using Backend.Enumerations;

namespace Backend.Models
{
    public abstract record ITelemetry
    {
        public DeviceType DeviceType { get; init; }

        public int WiFiSignal { get; init; }

        public int FreeHeap { get; init; }
    }
}

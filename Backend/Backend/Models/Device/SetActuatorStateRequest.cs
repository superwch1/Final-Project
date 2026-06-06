using Backend.Enumerations;

namespace Backend.Models
{
    public record SetActuatorStateRequest : BaseDeviceRequest
    {
        public ActuatorState ActuatorState { get; init; }
    }
}

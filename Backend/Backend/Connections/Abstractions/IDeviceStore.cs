using Backend.Enumerations;
using Backend.Models;

namespace Backend.Connections
{
    public interface IDeviceStore
    {
        /// <summary>
        /// Returns true if the device has saved to the database
        /// </summary>
        bool IsRegistered(string macAddress);

        /// <summary>
        /// Records that the device has saved to the database
        /// </summary>
        void MarkRegistered(string macAddress);

        /// <summary>
        /// Gets the cached policies for a sensor
        /// </summary>
        bool TryGetPolicies(string sensorMacAddress, out List<Policy>? policies);

        /// <summary>
        /// Caches the policies for a sensor
        /// </summary>
        void SetPolicies(string sensorMacAddress, List<Policy> policies);

        /// <summary>
        /// Clears the cached policies for a sensor so they are reloaded next time
        /// </summary>
        void ForgetPolicies(string sensorMacAddress);

        /// <summary>
        /// Gets the cached owner account of a device, where Guid.Empty means unpaired
        /// </summary>
        bool TryGetOwner(string macAddress, out Guid accountId);

        /// <summary>
        /// Caches the owner account of a device
        /// </summary>
        void SetOwner(string macAddress, Guid accountId);

        /// <summary>
        /// Clears the cached owner of a device so it is reloaded next time
        /// </summary>
        void ForgetOwner(string macAddress);

        /// <summary>
        /// Returns the MAC address of every device that has sent telemetry
        /// </summary>
        IEnumerable<string> GetMacAddresses();

        /// <summary>
        /// Gets the latest telemetry received from a device
        /// </summary>
        bool TryGetTelemetry(string macAddress, out BaseTelemetry? telemetry);

        /// <summary>
        /// Returns the stored actuator state
        /// </summary>
        ActuatorState GetActuatorState(string macAddress);

        /// <summary>
        /// Stores the actuator state, returning true if it changed
        /// </summary>
        Task<bool> SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken);

        /// <summary>
        /// Stores the latest telemetry, returning true if it is new or different from the previous one
        /// </summary>
        bool RecordTelemetry(string macAddress, BaseTelemetry telemetry);
    }
}

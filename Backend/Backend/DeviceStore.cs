using Backend.Enumerations;
using Backend.Models;
using System.Collections.Concurrent;

namespace Backend
{
    public class DeviceStore
    {
        const ActuatorState DefaultActuatorState = ActuatorState.Off;

        private readonly ConcurrentDictionary<string, ActuatorState> _actuatorStateByMacAddress = new();
        private readonly ConcurrentDictionary<string, BaseTelemetry> _telemetryByMacAddress = new();
        private readonly ConcurrentDictionary<string, Guid> _ownerByMacAddress = new();


        /// <summary>
        /// The cached owner of a device.
        /// </summary>
        public bool TryGetOwner(string macAddress, out Guid accountId)
        {
            return _ownerByMacAddress.TryGetValue(macAddress, out accountId);
        }

        /// <summary>
        /// Set who owns a device. 
        /// </summary>
        public void SetOwner(string macAddress, Guid accountId)
        {
            _ownerByMacAddress[macAddress] = accountId;
        }

        /// <summary>
        /// Forget the device owner.
        /// </summary>
        public void ForgetOwner(string macAddress)
        {
            _ownerByMacAddress.TryRemove(macAddress, out _);
        }

        public IEnumerable<string> GetMacAddresses()
        {
            return _telemetryByMacAddress.ToArray().Select(x => x.Key);
        }

        public bool TryGetTelemetry(string macAddress, out BaseTelemetry? telemetry)
        {
            return _telemetryByMacAddress.TryGetValue(macAddress, out telemetry);
        }

        public ActuatorState GetActuatorState(string macAddress)
        {
            return _actuatorStateByMacAddress.GetOrAdd(macAddress, _ => DefaultActuatorState);
        }

        public async Task<bool> SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            bool hasStateChanged = false;
            _actuatorStateByMacAddress.AddOrUpdate(macAddress, actuatorState, (_, oldActuatorState) =>
            {
                hasStateChanged = oldActuatorState != actuatorState;
                return actuatorState;
            });

            return hasStateChanged;
        }

        public bool RecordTelemetry(string macAddress, BaseTelemetry telemetry)
        {
            // default is true since when device just connected, it is Add 
            bool hasChanged = true;
            _telemetryByMacAddress.AddOrUpdate(macAddress, telemetry, (_, oldTelemetry) =>
            {
                hasChanged = HasTelemetryChanged(telemetry, oldTelemetry);
                return telemetry;
            });

            return hasChanged;
        }

        private static bool HasTelemetryChanged(BaseTelemetry telemetry, BaseTelemetry oldTelemetry)
        {
            if (telemetry is TempAndHumidTelemetry tempAndHumidTelemetry && oldTelemetry is TempAndHumidTelemetry oldTempAndHumidTelemetry)
                return tempAndHumidTelemetry != oldTempAndHumidTelemetry;

            else if (telemetry is LightTelemetry lightTelemetry && oldTelemetry is LightTelemetry oldLightTelemetry)
                return lightTelemetry != oldLightTelemetry;

            else if (telemetry is LedTelemetry ledTelemetry && oldTelemetry is LedTelemetry oldLedTelemetry)
                return ledTelemetry != oldLedTelemetry;

            else if (telemetry is FanTelemetry fanTelemetry && oldTelemetry is FanTelemetry oldFanTelemetry)
                return fanTelemetry != oldFanTelemetry;

            return false;
        }
    }
}

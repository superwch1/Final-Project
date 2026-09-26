using Backend.Enumerations;
using Backend.Models;
using System.Collections.Concurrent;

namespace Backend.Connections
{
    public sealed class DeviceStore : IDeviceStore
    {
        const ActuatorState DefaultActuatorState = ActuatorState.Off;

        private readonly ConcurrentDictionary<string, ActuatorState> _actuatorStateByMacAddress = new();
        private readonly ConcurrentDictionary<string, BaseTelemetry> _telemetryByMacAddress = new();
        private readonly ConcurrentDictionary<string, Guid> _ownerByMacAddress = new();
        private readonly ConcurrentDictionary<string, List<Policy>> _policiesBySensorMacAddress = new();

        private readonly ConcurrentDictionary<string, object?> _registeredMacAddresses = new();


        /// <inheritdoc/>
        public bool IsRegistered(string macAddress)
        {
            return _registeredMacAddresses.ContainsKey(macAddress);
        }

        /// <inheritdoc/>
        public void MarkRegistered(string macAddress)
        {
            _registeredMacAddresses.TryAdd(macAddress, null);
        }


        /// <inheritdoc/>
        public bool TryGetPolicies(string sensorMacAddress, out List<Policy>? policies)
        {
            return _policiesBySensorMacAddress.TryGetValue(sensorMacAddress, out policies);
        }


        /// <inheritdoc/>
        public void SetPolicies(string sensorMacAddress, List<Policy> policies)
        {
            _policiesBySensorMacAddress[sensorMacAddress] = policies;
        }


        /// <inheritdoc/>
        public void ForgetPolicies(string sensorMacAddress)
        {
            _policiesBySensorMacAddress.TryRemove(sensorMacAddress, out _);
        }


        /// <inheritdoc/>
        public bool TryGetOwner(string macAddress, out Guid accountId)
        {
            return _ownerByMacAddress.TryGetValue(macAddress, out accountId);
        }


        /// <inheritdoc/>
        public void SetOwner(string macAddress, Guid accountId)
        {
            _ownerByMacAddress[macAddress] = accountId;
        }


        /// <inheritdoc/>
        public void ForgetOwner(string macAddress)
        {
            _ownerByMacAddress.TryRemove(macAddress, out _);
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetMacAddresses()
        {
            return _telemetryByMacAddress.ToArray().Select(x => x.Key);
        }

        /// <inheritdoc/>
        public bool TryGetTelemetry(string macAddress, out BaseTelemetry? telemetry)
        {
            return _telemetryByMacAddress.TryGetValue(macAddress, out telemetry);
        }

        /// <inheritdoc/>
        public ActuatorState GetActuatorState(string macAddress)
        {
            return _actuatorStateByMacAddress.GetOrAdd(macAddress, _ => DefaultActuatorState);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Returns true if two telemetry values of the same type differ
        /// </summary>
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

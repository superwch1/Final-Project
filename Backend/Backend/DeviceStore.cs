using Backend.Connections;
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

        private readonly DeviceConnections _deviceConnections;

        public DeviceStore(DeviceConnections deviceConnections)
        {
            _deviceConnections = deviceConnections;
        }


        public ActuatorState GetActuatorState(string macAddress)
        {
            return _actuatorStateByMacAddress.GetOrAdd(macAddress, _ => DefaultActuatorState);
        }


        public async Task SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            bool hasStateChanged = false;
            _actuatorStateByMacAddress.AddOrUpdate(macAddress, actuatorState, (_, oldActuatorState) =>
            {
                hasStateChanged = oldActuatorState != actuatorState;
                return actuatorState;
            });

            if (!hasStateChanged)
                return;

            await _deviceConnections.NotifyActuatorState(macAddress, actuatorState, cancellationToken);

            // also need to notify dashboard connection
        }


        public void UploadTelemetry(string macAddress, BaseTelemetry telemetry)
        {
            bool hasChanged = false;
            _telemetryByMacAddress.AddOrUpdate(macAddress, telemetry, (_, oldTelemetry) =>
            {
                hasChanged = HasReadingChanged(telemetry, oldTelemetry) || HasStateChanged(telemetry, oldTelemetry);
                return telemetry;
            });

            if (!hasChanged)
                return;

            // notify dashboard connection reading has changed
        }


        private static bool HasReadingChanged(BaseTelemetry telemetry, BaseTelemetry oldTelemetry)
        {
            if (telemetry is TempAndHumidTelemetry tempAndHumidTelemetry && oldTelemetry is TempAndHumidTelemetry oldTempAndHumidTelemetry)
            {
                return
                    tempAndHumidTelemetry.TemperatureReading != oldTempAndHumidTelemetry.TemperatureReading ||
                    tempAndHumidTelemetry.HumidityReading != oldTempAndHumidTelemetry.HumidityReading;
            }
            else if (telemetry is LightTelemetry lightTelemetry && oldTelemetry is LightTelemetry oldLightTelemetry)
            {
                return lightTelemetry.LightReading != oldLightTelemetry.LightReading;
            }

            return false;
        }


        private static bool HasStateChanged(BaseTelemetry telemetry, BaseTelemetry oldTelemetry)
        {
            if (telemetry is LedActuatorTelemetry ledTelemetry && oldTelemetry is LedActuatorTelemetry oldLedTelemetry)
            {
                return ledTelemetry.ActuatorState != oldLedTelemetry.ActuatorState;
            }
            else if (telemetry is FanActuatorTelemetry fanTelemetry && oldTelemetry is FanActuatorTelemetry oldFanTelemetry)
            {
                return fanTelemetry.ActuatorState != oldFanTelemetry.ActuatorState;
            }

            return false;
        }
    }
}

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
        }


        public void UploadTelemetry(string macAddress, BaseTelemetry telemetry)
        {
            bool hasReadingChanged = false;
            _telemetryByMacAddress.AddOrUpdate(macAddress, telemetry, (_, oldTelemetry) =>
            {
                hasReadingChanged = HasReadingChanged(telemetry, oldTelemetry);
                return telemetry;
            });

            if (!hasReadingChanged)
                return;

            // notify dashboard connection reading has changed
        }


        private bool HasReadingChanged(BaseTelemetry telemetry, BaseTelemetry oldTelemetry)
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
    }
}

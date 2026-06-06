using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using System.Net.WebSockets;

namespace Backend
{
    public class ConnectionMediator
    {
        private readonly DeviceConnections _deviceConnections;
        private readonly DeviceStore _deviceStore;

        public ConnectionMediator(DeviceConnections deviceConnections, DeviceStore deviceStore)
        {
            _deviceConnections = deviceConnections;
            _deviceStore = deviceStore;

            _deviceConnections.SubscribeToTelemetryReceived(OnTelemetryReceived);
            // dashboard connection (OnActuatorStateSet)
        }


        public async Task SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            bool hasChanged = await _deviceStore.SetActuatorState(macAddress, actuatorState, cancellationToken);
            if (hasChanged)
                await _deviceConnections.NotifyActuatorState(macAddress, actuatorState, cancellationToken);
        }

        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken)
        {
            ActuatorState? actuatorState = null;
            if (deviceType.IsActuator())
                actuatorState = _deviceStore.GetActuatorState(macAddress);

            List<string> initialMessages = actuatorState is null ? [] : [ actuatorState.ToString()! ];
            await _deviceConnections.DeviceEcho(webSocket, macAddress, deviceType, initialMessages, cancellationToken);
        }

        private void OnTelemetryReceived(object? sender, (string MacAddress, BaseTelemetry Telemetry) eventArgs)
        {
            bool hasChanged = _deviceStore.RecordTelemetry(eventArgs.MacAddress, eventArgs.Telemetry);
            // notify dashboard connection telemety has changed
        }
    }
}

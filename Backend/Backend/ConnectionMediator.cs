using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend
{
    public class ConnectionMediator
    {
        private readonly DeviceConnections _deviceConnections;
        private readonly DashboardConnections _dashboardConnections;
        private readonly DeviceStore _deviceStore;

        public ConnectionMediator(DeviceConnections deviceConnections, DashboardConnections dashboardConnections, DeviceStore deviceStore)
        {
            _dashboardConnections = dashboardConnections;
            _deviceConnections = deviceConnections;
            _deviceStore = deviceStore;

            _deviceConnections.SubscribeToTelemetryReceived(OnTelemetryReceived);
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

        public async Task DashboardEcho(WebSocket webSocket, CancellationToken cancellationToken)
        {
            List<string> initialMessages = [];
            IEnumerable<string> macAddresses = _deviceStore.GetMacAddresses(); 

            foreach (string macAddress in macAddresses)
            {
                if (_deviceStore.TryGetTelemetry(macAddress, out BaseTelemetry? telemetry) && telemetry != null &&
                    TrySerializeTelemetry(telemetry, out string? message) && message != null)
                {
                    initialMessages.Add(message);
                }
                
            }

            await _dashboardConnections.DashboardEcho(webSocket, initialMessages, cancellationToken);
        }

        private async Task OnTelemetryReceived((string MacAddress, BaseTelemetry Telemetry) eventArgs)
        {
            bool hasChanged = _deviceStore.RecordTelemetry(eventArgs.MacAddress, eventArgs.Telemetry);
            if (hasChanged && TrySerializeTelemetry(eventArgs.Telemetry, out string? message) && message != null)
            {
                await _dashboardConnections.NotifyTelemetryChanged(message, CancellationToken.None);
            }
        }

        private static bool TrySerializeTelemetry(BaseTelemetry telemetry, out string? message)
        {
            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());

            switch (telemetry)
            {
                case LedTelemetry ledTelemetry:
                    message = JsonSerializer.Serialize(ledTelemetry, options);
                    return true;

                case FanTelemetry fanTelemetry:
                    message = JsonSerializer.Serialize(fanTelemetry, options);
                    return true;

                case LightTelemetry lightTelemetry:
                    message = JsonSerializer.Serialize(lightTelemetry, options);
                    return true;

                case TempAndHumidTelemetry tempAndHumidTelemetry:
                    message = JsonSerializer.Serialize(tempAndHumidTelemetry, options);
                    return true;
            }

            message = null;
            return false;
        }
    }
}

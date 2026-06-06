using Backend.Enumerations;
using Backend.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Backend.Connections
{
    public class DeviceConnections
    {
        private readonly DeviceStore _deviceStore;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _connectionsByMacAddress = new();    

        public DeviceConnections(DeviceStore deviceStore)
        {
            _deviceStore = deviceStore;
        }


        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();
            try
            {
                AddDeviceConnection(connectionId, macAddress, webSocket);

                if (deviceType.IsActuator())
                {
                    ActuatorState actuatorState = _deviceStore.GetActuatorState(macAddress);
                    await NotifyActuatorState(macAddress, actuatorState, cancellationToken);
                }

                byte[] buffer = new byte[1024 * 4];
                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                while (!receiveResult.CloseStatus.HasValue)
                {
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    string message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    UpdateDeviceTelemetry(deviceType, macAddress, message);
                }

                await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, cancellationToken);
            }
            finally
            {
                RemoveDeviceConnection(connectionId, macAddress);
            }
        }


        public async Task NotifyActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            if (_connectionsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, WebSocket>? connections) && connections != null)
            {
                foreach (WebSocket webSocket in connections.Values.Where(x => x.State == WebSocketState.Open))
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(actuatorState.ToString());
                    await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }


        private void AddDeviceConnection(Guid connectionId, string macAddress, WebSocket webSocket)
        {
            ConcurrentDictionary<Guid, WebSocket> connections = _connectionsByMacAddress.GetOrAdd(macAddress, _ => new ConcurrentDictionary<Guid, WebSocket>());
            connections.TryAdd(connectionId, webSocket);
        }


        private void RemoveDeviceConnection(Guid connectionId, string macAddress)
        {
            if (_connectionsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, WebSocket>? connections) && connections != null)
            {
                // can be possible that add and remove at the same time so never remove macAddress even when it is empty
                connections.TryRemove(connectionId, out _);
            }
        }


        private void UpdateDeviceTelemetry(DeviceType deviceType, string macAddress, string message)
        {
            try
            {
                BaseTelemetry? telemetry = null;
                JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                switch (deviceType)
                {
                    case DeviceType.TempAndHumidSensor:
                        telemetry = JsonSerializer.Deserialize<TempAndHumidTelemetry>(message, options);
                        break; ;

                    case DeviceType.LightSensor:
                        telemetry = JsonSerializer.Deserialize<LightTelemetry>(message, options);
                        break;

                    case DeviceType.FanActuator:
                        telemetry = JsonSerializer.Deserialize<FanActuatorTelemetry>(message, options);
                        break;

                    case DeviceType.LedActuator:
                        telemetry = JsonSerializer.Deserialize<LedActuatorTelemetry>(message, options);
                        break;
                }

                if (telemetry != null)
                    _deviceStore.UploadTelemetry(macAddress, telemetry);
            }
            catch
            {
                // De-serialization failed
            }
        }
    }
}

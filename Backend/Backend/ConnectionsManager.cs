using Backend.Enumerations;
using Backend.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Backend
{
    public class ConnectionsManager
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _webSocketByConnectionId = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, object?>> _connectionIdsByMacAddress = new();

        // this two is not relevant for connection, only for storing the state
        private readonly ConcurrentDictionary<string, ActuatorState> _actuatorStateByMacAddress = new();
        private readonly ConcurrentDictionary<string, ITelemetry> _telemetryByMacAddress = new();

        private void AddDeviceConnection(Guid connectionId, string macAddress, WebSocket webSocket)
        {
            _webSocketByConnectionId.TryAdd(connectionId, webSocket);

            ConcurrentDictionary<Guid, object?> connectionIds = _connectionIdsByMacAddress.GetOrAdd(macAddress, _ => new ConcurrentDictionary<Guid, object?>());
            connectionIds.TryAdd(connectionId, null);
        }


        private void RemoveDeviceConnection(Guid connectionId, string macAddress)
        {
            if (_webSocketByConnectionId.TryRemove(connectionId, out _) &&
                _connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds))
            {
                connectionIds.TryRemove(connectionId, out _);
                /* can be possible that add and remove at the same time
                if (connectionIds.IsEmpty)
                {
                    _connectedIdsByMacAddress.TryRemove(macAddress, out _);
                }*/
            }
        }


        public async Task SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            _actuatorStateByMacAddress.AddOrUpdate(macAddress, actuatorState, (_, _) => actuatorState);
            await NotifyActuatorState(macAddress, actuatorState, cancellationToken);
        }


        private async Task NotifyActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            if (_connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds))
            {
                foreach (Guid connectionId in connectionIds.Keys)
                {
                    if (_webSocketByConnectionId.TryGetValue(connectionId, out WebSocket? webSocket) && webSocket.State == WebSocketState.Open)
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(actuatorState.ToString());
                        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
                    }
                }
            }
        }


        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();
            try
            {
                AddDeviceConnection(connectionId, macAddress, webSocket);

                if (deviceType.IsActuator())
                {
                    ActuatorState actuatorState = _actuatorStateByMacAddress.GetOrAdd(macAddress, _ => ActuatorState.Off);
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


        private void UpdateDeviceTelemetry(DeviceType deviceType, string macAddress, string message)
        {
            Console.WriteLine(message);
            try
            {
                ITelemetry? telemetry = null;
                JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                switch (deviceType)
                {
                    case DeviceType.TempSensor:
                        break;

                    case DeviceType.LightSensor:
                        break;

                    case DeviceType.LedActuator:
                        telemetry = JsonSerializer.Deserialize<LedTelemetry>(message, options);
                        break;
                }

                if (telemetry != null)
                    _telemetryByMacAddress.AddOrUpdate(macAddress, telemetry, (_, _) => telemetry);
            }
            catch
            {
                // De-serialization failed
            }
        }
    }
}

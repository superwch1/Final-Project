using Backend.Enumerations;
using Backend.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace Backend.Connections
{
    public sealed class DeviceConnections : BaseConnection
    {
        private readonly ConcurrentDictionary<Guid, DeviceType> _deviceTypeByConnectionId = new();
        private readonly ConcurrentDictionary<Guid, string> _macAddressByConnectionId = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, object?>> _connectionIdsByMacAddress = new();

        private readonly DeviceStore _deviceStore;

        public DeviceConnections(DeviceStore deviceStore)
        {
            _deviceStore = deviceStore;
        }


        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();

            try
            {
                AddDeviceConnection(connectionId, macAddress, deviceType);

                string initialMessage = "";
                if (deviceType.IsActuator())
                {
                    ActuatorState actuatorState = _deviceStore.GetActuatorState(macAddress);
                    initialMessage = actuatorState.ToString();
                }

                await Echo(connectionId, webSocket, cancellationToken, initialMessage: initialMessage);
            }
            finally
            {
                RemoveDeviceConnection(connectionId, macAddress);
            }
        }

        public async Task NotifyActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            if (_connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds) && connectionIds != null)
            {
                await SendMessage(connectionIds.Keys, actuatorState.ToString(), cancellationToken);
            }
        }

        protected override async Task OnMessageReceived(Guid connectionId, string message)
        {
            if (_deviceTypeByConnectionId.TryGetValue(connectionId, out DeviceType deviceType) &&
                _macAddressByConnectionId.TryGetValue(connectionId, out string? macAddress) && macAddress != null)
            {
                try
                {
                    BaseTelemetry? telemetry = null;
                    JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                    switch (deviceType)
                    {
                        case DeviceType.TempAndHumidSensor:
                            telemetry = JsonSerializer.Deserialize<TempAndHumidTelemetry>(message, options);
                            break;

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

        private void AddDeviceConnection(Guid connectionId, string macAddress, DeviceType deviceType)
        {
            _deviceTypeByConnectionId.TryAdd(connectionId, deviceType);
            _macAddressByConnectionId.TryAdd(connectionId, macAddress);

            ConcurrentDictionary<Guid, object?> connectionIds = _connectionIdsByMacAddress.GetOrAdd(macAddress, _ => new ConcurrentDictionary<Guid, object?>());
            connectionIds.TryAdd(connectionId, null);
        }

        private void RemoveDeviceConnection(Guid connectionId, string macAddress)
        {
            _deviceTypeByConnectionId.TryRemove(connectionId, out _);
            _macAddressByConnectionId.TryRemove(connectionId, out _);

            if (_connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds) && connectionIds != null)
            {
                // can be possible that add and remove at the same time so never remove macAddress even when it is empty
                connectionIds.TryRemove(connectionId, out _);
            }
        }
    }
}

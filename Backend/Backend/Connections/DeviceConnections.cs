using Backend.Enumerations;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Backend.Connections
{
    public sealed class DeviceConnections : BaseConnections
    {
        private readonly ConcurrentDictionary<Guid, DeviceType> _deviceTypeByConnectionId = new();
        private readonly ConcurrentDictionary<Guid, string> _macAddressByConnectionId = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, object?>> _connectionIdsByMacAddress = new();
        private readonly ConcurrentDictionary<Guid, long> _lastTimestampByConnectionId = new();

        private readonly DeviceOptions _options;
        private readonly IDeviceKeyService _deviceKeyService;

        public DeviceConnections(IOptions<DeviceOptions> options, IDeviceKeyService deviceKeyService)
        {
            _options = options.Value;
            _deviceKeyService = deviceKeyService;
        }

        private event Func<(string MacAddress, BaseTelemetry Telemetry), Task>? _telemetryReceived;


        /// <summary>
        /// Registers a device connection and runs it until it closes, then removes it
        /// </summary>
        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, IEnumerable<string> initialMessages, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();
            try
            {
                AddDeviceConnection(connectionId, macAddress, deviceType);
                await Echo(connectionId, webSocket, initialMessages, cancellationToken);
            }
            finally
            {
                RemoveDeviceConnection(connectionId, macAddress);
            }
        }

        /// <summary>
        /// Sends the actuator state to every connection for the given MAC address
        /// </summary>
        public async Task NotifyActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            if (_connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds) && connectionIds != null)
            {
                foreach(Guid connectionId in connectionIds.ToArray().Select(x => x.Key))
                {
                    await SendMessageAsync(connectionId, actuatorState.ToString(), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Builds a JSON message containing the current server time in Unix milliseconds
        /// </summary>
        public static string ServerTimeMessage()
        {
            return $"{{\"serverTime\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
        }

        /// <summary>
        /// Sends the current server time to every connected device
        /// </summary>
        public Task BroadcastServerTime(CancellationToken cancellationToken)
        {
            return SendMessageToAllAsync(ServerTimeMessage(), cancellationToken);
        }

        /// <summary>
        /// Registers a handler that runs when valid telemetry is received from a device
        /// </summary>
        public void SubscribeToTelemetryReceived(Func<(string MacAddress, BaseTelemetry Telemetry), Task> eventHandler)
            => _telemetryReceived += eventHandler;

        /// <summary>
        /// Verifies the timestamp and HMAC signature of a device message
        /// </summary>
        protected override async Task OnMessageReceived(Guid connectionId, string message)
        {
            if (!_deviceTypeByConnectionId.TryGetValue(connectionId, out DeviceType deviceType) ||
                !_macAddressByConnectionId.TryGetValue(connectionId, out string? macAddress) || macAddress == null)
            {
                return;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(message);
                JsonElement root = document.RootElement;

                Console.WriteLine($"Message - {macAddress}");
                Console.WriteLine(JsonSerializer.Serialize(root, new JsonSerializerOptions() { WriteIndented = true }));

                long timestamp = root.GetProperty("timestamp").GetInt64();
                string signature = root.GetProperty("signature").GetString() ?? "";
                JsonElement data = root.GetProperty("data");

                // check is the message valid
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                bool hasValidTimestamp = Math.Abs(now - timestamp) <= (long)_options.MaxClockSkew.TotalMilliseconds;

                byte[] key = Encoding.UTF8.GetBytes(_deviceKeyService.Derive(_options.MasterKey, macAddress));
                string signed = $"{_deviceKeyService.NormalizeMacAddress(macAddress)}|{deviceType}|{timestamp}|{data}";

                byte[] received = Convert.FromHexString(signature);
                byte[] expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signed));

                bool hasValidSignature = CryptographicOperations.FixedTimeEquals(expected, received);

                if(!hasValidTimestamp || !hasValidSignature)
                {
                    return;
                }

                // does not accept previous accepted timestamp
                if (_lastTimestampByConnectionId.GetOrAdd(connectionId, 0) >= timestamp)
                {
                    return;
                }

                _lastTimestampByConnectionId[connectionId] = timestamp;

                BaseTelemetry? telemetry = null;
                JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                switch (deviceType)
                {
                    case DeviceType.TempAndHumidSensor:
                        telemetry = data.Deserialize<TempAndHumidTelemetry>(options);
                        break;

                    case DeviceType.LightSensor:
                        telemetry = data.Deserialize<LightTelemetry>(options);
                        break;

                    case DeviceType.FanActuator:
                        telemetry = data.Deserialize<FanTelemetry>(options);
                        break;

                    case DeviceType.LedActuator:
                        telemetry = data.Deserialize<LedTelemetry>(options);
                        break;
                }

                if (_telemetryReceived != null && telemetry != null)
                {
                    await _telemetryReceived.Invoke((macAddress, telemetry));
                }
            }
            catch
            {
                // De-serialization failed
            }
        }

        /// <summary>
        /// Records the device type and MAC address for a new connection
        /// </summary>
        private void AddDeviceConnection(Guid connectionId, string macAddress, DeviceType deviceType)
        {
            _deviceTypeByConnectionId.TryAdd(connectionId, deviceType);
            _macAddressByConnectionId.TryAdd(connectionId, macAddress);

            ConcurrentDictionary<Guid, object?> connectionIds = _connectionIdsByMacAddress.GetOrAdd(macAddress, _ => new ConcurrentDictionary<Guid, object?>());
            connectionIds.TryAdd(connectionId, null);
        }

        /// <summary>
        /// Removes all tracking data for a closed connection
        /// </summary>
        private void RemoveDeviceConnection(Guid connectionId, string macAddress)
        {
            _deviceTypeByConnectionId.TryRemove(connectionId, out _);
            _macAddressByConnectionId.TryRemove(connectionId, out _);
            _lastTimestampByConnectionId.TryRemove(connectionId, out _);

            if (_connectionIdsByMacAddress.TryGetValue(macAddress, out ConcurrentDictionary<Guid, object?>? connectionIds) && connectionIds != null)
            {
                // can be possible that add and remove at the same time so never remove macAddress even when it is empty
                connectionIds.TryRemove(connectionId, out _);
            }
        }
    }
}

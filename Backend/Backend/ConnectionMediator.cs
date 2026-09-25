using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConnectionMediator(DeviceConnections deviceConnections, DashboardConnections dashboardConnections, DeviceStore deviceStore, IServiceScopeFactory serviceScopeFactory)
        {
            _dashboardConnections = dashboardConnections;
            _deviceConnections = deviceConnections;
            _deviceStore = deviceStore;
            _serviceScopeFactory = serviceScopeFactory;

            _deviceConnections.SubscribeToTelemetryReceived(OnTelemetryReceived);
            _dashboardConnections.SubscribeToDashboardSignedIn(OnDashboardSignedIn);
        }


        public async Task SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            macAddress = DeviceKey.NormalizeMacAddress(macAddress);

            bool hasChanged = await _deviceStore.SetActuatorState(macAddress, actuatorState, cancellationToken);
            if (hasChanged)
                await _deviceConnections.NotifyActuatorState(macAddress, actuatorState, cancellationToken);
        }

        public async Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken)
        {
            macAddress = DeviceKey.NormalizeMacAddress(macAddress);

            // send the unix time to the board
            List<string> initialMessages = [DeviceConnections.ServerTimeMessage()];

            // send the state to the actuator
            if (deviceType.IsActuator())
                initialMessages.Add(_deviceStore.GetActuatorState(macAddress).ToString());

            await _deviceConnections.DeviceEcho(webSocket, macAddress, deviceType, initialMessages, cancellationToken);
        }

        public async Task DashboardEcho(WebSocket webSocket, CancellationToken cancellationToken)
        {
            await _dashboardConnections.DashboardEcho(webSocket, cancellationToken);
        }

        private async Task OnDashboardSignedIn((Guid ConnectionId, Guid AccountId) eventArgs)
        {
            List<string> initialMessages = [];

            foreach (string macAddress in await FindMacAddressesAsync(eventArgs.AccountId))
            {
                if (_deviceStore.TryGetTelemetry(macAddress, out BaseTelemetry? telemetry) && telemetry != null &&
                    TrySerializeTelemetry(telemetry, out string? message) && message != null)
                {
                    initialMessages.Add(message);
                }
            }

            await _dashboardConnections.SendSnapshot(eventArgs.ConnectionId, initialMessages, CancellationToken.None);
        }

        private async Task OnTelemetryReceived((string MacAddress, BaseTelemetry Telemetry) eventArgs)
        {
            await RegisterDeviceAsync(eventArgs.MacAddress, eventArgs.Telemetry.DeviceType);

            bool hasChanged = _deviceStore.RecordTelemetry(eventArgs.MacAddress, eventArgs.Telemetry);
            if (!hasChanged || !TrySerializeTelemetry(eventArgs.Telemetry, out string? message) || message == null)
            {
                return;
            }


            Guid? accountId = await FindOwnerAccountIdAsync(eventArgs.MacAddress);
            if (accountId is null)
            {
                return;
            }

            await _dashboardConnections.NotifyTelemetryChanged(message, accountId.Value, CancellationToken.None);
            await ApplyPoliciesAsync(eventArgs.MacAddress, eventArgs.Telemetry);
        }

        /// <summary>
        /// Switch the actuators this sensor drives.
        /// </summary>
        private async Task ApplyPoliciesAsync(string sensorMacAddress, BaseTelemetry telemetry)
        {
            foreach (Policy policy in await FindPoliciesAsync(sensorMacAddress))
            {
                // A disabled policy means the user is driving that actuator manually
                if (!policy.IsEnabled)
                {
                    continue;
                }

                double? reading = ReadValue(telemetry, policy.Reading);

                if (reading is null)
                {
                    continue;
                }

                bool isConditionMet = (policy.Comparison == Comparison.Above)
                    ? (reading.Value > policy.Threshold)
                    : (reading.Value < policy.Threshold);

                ActuatorState state = isConditionMet
                    ? policy.ActuatorState
                    : Opposite(policy.ActuatorState);

                await SetActuatorState(policy.ActuatorMacAddress, state, CancellationToken.None);
            }
        }

        private static ActuatorState Opposite(ActuatorState actuatorState)
        {
            return (actuatorState == ActuatorState.On) ? ActuatorState.Off : ActuatorState.On;
        }

        /// <summary>
        /// The value a policy watches, or null when this telemetry does not carry it.
        /// </summary>
        private static double? ReadValue(BaseTelemetry telemetry, SensorReading reading)
        {
            return (telemetry, reading) switch
            {
                (TempAndHumidTelemetry sensor, SensorReading.Temperature) => sensor.TemperatureReading,
                (TempAndHumidTelemetry sensor, SensorReading.Humidity) => sensor.HumidityReading,
                (LightTelemetry sensor, SensorReading.Light) => sensor.LightReading,
                _ => null
            };
        }

        private async Task<List<Policy>> FindPoliciesAsync(string sensorMacAddress)
        {
            if (_deviceStore.TryGetPolicies(sensorMacAddress, out List<Policy>? cached) && cached is not null)
            {
                return cached;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IPolicyRepository policyRepository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();

            List<Policy> policies = await policyRepository.FindBySensorAsync(sensorMacAddress, CancellationToken.None);

            // Sensors with no policy are cached too, so they stop hitting the database.
            _deviceStore.SetPolicies(sensorMacAddress, policies);

            return policies;
        }

        /// <summary>
        /// Add a device the first time it reports
        /// </summary>
        private async Task RegisterDeviceAsync(string macAddress, DeviceType deviceType)
        {
            if (_deviceStore.IsRegistered(macAddress))
            {
                return;
            }

            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IDeviceRepository deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

            Device? device = await deviceRepository.FindByMacAddressAsync(macAddress, CancellationToken.None);

            if (device is null)
            {
                await deviceRepository.TryAddAsync(new Device
                {
                    MacAddress = macAddress,
                    RoomId = null,
                    Name = deviceType.ToString(),
                    DeviceType = deviceType
                }, CancellationToken.None);
            }
            else if (device.DeviceType != deviceType)
            {
                // The jumpers were changed and the board reports something new.
                device.DeviceType = deviceType;
                await deviceRepository.UpdateAsync(device, CancellationToken.None);
            }

            _deviceStore.MarkRegistered(macAddress);
        }

        /// <summary>
        /// The account whose room the device is paired into, or null when unpaired.
        /// </summary>
        private async Task<Guid?> FindOwnerAccountIdAsync(string macAddress)
        {
            if (_deviceStore.TryGetOwner(macAddress, out Guid cached))
            {
                return (cached == Guid.Empty) ? null : cached;
            }

            Guid owner = await ReadOwnerAccountIdAsync(macAddress);
            _deviceStore.SetOwner(macAddress, owner);

            return (owner == Guid.Empty) ? null : owner;
        }

        /// <summary>
        /// Read the owner from the database, or Guid.Empty when the device is unpaired.
        /// </summary>
        private async Task<Guid> ReadOwnerAccountIdAsync(string macAddress)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            IDeviceRepository deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            Device? device = await deviceRepository.FindByMacAddressAsync(macAddress, CancellationToken.None);

            if (device is null)
            {
                return Guid.Empty;
            }

            if (device.RoomId is null)
            {
                return Guid.Empty;
            }

            IRoomRepository roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
            Room? room = await roomRepository.FindByIdAsync(device.RoomId.Value, CancellationToken.None);

            return room?.AccountId ?? Guid.Empty;
        }

        /// <summary>
        /// The normalized MAC of every device paired into the account's rooms.
        /// </summary>
        private async Task<List<string>> FindMacAddressesAsync(Guid accountId)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            IRoomRepository roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
            List<Room> rooms = await roomRepository.FindByAccountAsync(accountId, CancellationToken.None);

            return rooms.SelectMany(room => room.Devices).Select(device => device.MacAddress).ToList();
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

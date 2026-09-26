using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Backend.Tests.Connections
{
    public sealed class ConnectionMediatorTests : IDisposable
    {
        private readonly Mock<IDeviceStore> _deviceStore = new();
        private readonly Mock<IDeviceRepository> _deviceRepository = new();
        private readonly Mock<IRoomRepository> _roomRepository = new();
        private readonly Mock<IPolicyRepository> _policyRepository = new();
        private readonly ServiceProvider _serviceProvider;
        private readonly ConnectionMediator _connectionMediator;
        private readonly Guid _accountId = Guid.NewGuid();

        public ConnectionMediatorTests()
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(_deviceRepository.Object)
                .AddSingleton(_roomRepository.Object)
                .AddSingleton(_policyRepository.Object)
                .BuildServiceProvider();

            _connectionMediator = new ConnectionMediator(
                new DeviceConnections(Options.Create(TestOptions.Device), new DeviceKeyService()),
                new DashboardConnections(TestOptions.JwtBearer()),
                _deviceStore.Object,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new DeviceKeyService());
        }

        [Fact]
        public async Task SetActuatorState_MacAddressWithColons_StoresNormalisedAddress()
        {
            await _connectionMediator.SetActuatorState("aa:bb:cc:dd:ee:02", ActuatorState.On, CancellationToken.None);

            _deviceStore.Verify(x => x.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeviceEcho_Actuator_SendsServerTimeThenStoredState()
        {
            _deviceStore.Setup(x => x.GetActuatorState(TestData.ActuatorMacAddress)).Returns(ActuatorState.On);
            FakeWebSocket webSocket = new();

            await _connectionMediator.DeviceEcho(webSocket, TestData.ActuatorMacAddress, DeviceType.LedActuator, CancellationToken.None);

            Assert.Collection(webSocket.Sent,
                first => Assert.Contains("serverTime", first),
                second => Assert.Equal("On", second));
        }

        [Fact]
        public async Task DeviceEcho_Sensor_SendsOnlyServerTime()
        {
            FakeWebSocket webSocket = new();

            await _connectionMediator.DeviceEcho(webSocket, TestData.SensorMacAddress, DeviceType.LightSensor, CancellationToken.None);

            Assert.Contains("serverTime", Assert.Single(webSocket.Sent));
        }

        [Fact]
        public async Task Telemetry_NewDevice_AddsDeviceToDatabase()
        {
            await SendLightReadingAsync(80);

            _deviceRepository.Verify(x => x.TryAddAsync(
                It.Is<Device>(d => (d.MacAddress == TestData.SensorMacAddress) && (d.DeviceType == DeviceType.LightSensor)),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Telemetry_DeviceTypeChanged_UpdatesDeviceType()
        {
            Device device = TestData.Device(TestData.SensorMacAddress, DeviceType.TempAndHumidSensor);
            _deviceRepository.Setup(x => x.FindByMacAddressAsync(TestData.SensorMacAddress, It.IsAny<CancellationToken>())).ReturnsAsync(device);

            await SendLightReadingAsync(80);

            _deviceRepository.Verify(x => x.UpdateAsync(It.Is<Device>(d => d.DeviceType == DeviceType.LightSensor), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Telemetry_UnchangedReading_DoesNotLookUpOwner()
        {
            _deviceStore.Setup(x => x.IsRegistered(TestData.SensorMacAddress)).Returns(true);

            await SendLightReadingAsync(80);

            _deviceStore.Verify(x => x.TryGetOwner(It.IsAny<string>(), out It.Ref<Guid>.IsAny), Times.Never);
        }

        [Fact]
        public async Task Telemetry_UnpairedSensor_DoesNotApplyPolicies()
        {
            SetUpChangedReadingFromOwnedSensor(Guid.Empty);

            await SendLightReadingAsync(80);

            _deviceStore.Verify(x => x.TryGetPolicies(It.IsAny<string>(), out It.Ref<List<Policy>?>.IsAny), Times.Never);
        }

        [Fact]
        public async Task Telemetry_PoliciesNotCached_CachesPoliciesFromDatabase()
        {
            SetUpChangedReadingFromOwnedSensor(_accountId);
            List<Policy> policies = [];
            _policyRepository.Setup(x => x.FindBySensorAsync(TestData.SensorMacAddress, It.IsAny<CancellationToken>())).ReturnsAsync(policies);

            await SendLightReadingAsync(80);

            _deviceStore.Verify(x => x.SetPolicies(TestData.SensorMacAddress, policies));
        }

        [Fact]
        public async Task Telemetry_ReadingAboveThreshold_SwitchesActuatorOn()
        {
            SetUpChangedReadingFromOwnedSensor(_accountId);
            SetUpCachedPolicy(TestData.Policy());

            await SendLightReadingAsync(80);

            _deviceStore.Verify(x => x.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Telemetry_ReadingBelowThreshold_SwitchesActuatorOff()
        {
            SetUpChangedReadingFromOwnedSensor(_accountId);
            SetUpCachedPolicy(TestData.Policy());

            await SendLightReadingAsync(20);

            _deviceStore.Verify(x => x.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.Off, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Telemetry_DisabledPolicy_LeavesActuatorAlone()
        {
            SetUpChangedReadingFromOwnedSensor(_accountId);
            Policy policy = TestData.Policy();
            policy.IsEnabled = false;
            SetUpCachedPolicy(policy);

            await SendLightReadingAsync(80);

            _deviceStore.Verify(x => x.SetActuatorState(It.IsAny<string>(), It.IsAny<ActuatorState>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DashboardEcho_SignedIn_SendsLatestTelemetryOfOwnedDevices()
        {
            Room room = TestData.Room(_accountId);
            room.Devices.Add(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, room.Id));
            _roomRepository.Setup(x => x.FindByAccountAsync(_accountId, It.IsAny<CancellationToken>())).ReturnsAsync([room]);
            BaseTelemetry? telemetry = new LightTelemetry { MacAddress = TestData.SensorMacAddress, DeviceType = DeviceType.LightSensor, LightReading = 42 };
            _deviceStore.Setup(x => x.TryGetTelemetry(TestData.SensorMacAddress, out telemetry)).Returns(true);
            Account account = TestData.Account();
            account.Id = _accountId;
            FakeWebSocket webSocket = new([new JwtTokenService(Options.Create(TestOptions.Jwt)).CreateToken(account)]);

            await _connectionMediator.DashboardEcho(webSocket, CancellationToken.None);

            Assert.Contains("\"lightReading\":42", Assert.Single(webSocket.Sent));
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        /// <summary>
        /// Connects the test light sensor and sends one reading
        /// </summary>
        private Task SendLightReadingAsync(int lightReading)
        {
            string data = $"{{\"deviceType\":\"LightSensor\",\"lightReading\":{lightReading}}}";
            FakeWebSocket webSocket = new([TestOptions.SignedMessage(TestData.SensorMacAddress, DeviceType.LightSensor, TestOptions.Now(), data)]);

            return _connectionMediator.DeviceEcho(webSocket, TestData.SensorMacAddress, DeviceType.LightSensor, CancellationToken.None);
        }

        /// <summary>
        /// Register the test sensor
        /// </summary>
        private void SetUpChangedReadingFromOwnedSensor(Guid ownerAccountId)
        {
            _deviceStore.Setup(x => x.IsRegistered(TestData.SensorMacAddress)).Returns(true);
            _deviceStore.Setup(x => x.RecordTelemetry(TestData.SensorMacAddress, It.IsAny<BaseTelemetry>())).Returns(true);
            _deviceStore.Setup(x => x.TryGetOwner(TestData.SensorMacAddress, out ownerAccountId)).Returns(true);
        }

        /// <summary>
        /// Cache the policy
        /// </summary>
        private void SetUpCachedPolicy(Policy policy)
        {
            List<Policy>? policies = [policy];
            _deviceStore.Setup(x => x.TryGetPolicies(TestData.SensorMacAddress, out policies)).Returns(true);
        }
    }
}

using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Backend.Tests.Helpers;

namespace Backend.Tests.Connections
{
    public class DeviceStoreTests
    {
        private const string MacAddress = TestData.SensorMacAddress;

        private readonly DeviceStore _deviceStore = new();

        [Fact]
        public void IsRegistered_UnknownDevice_ReturnsFalse()
        {
            Assert.False(_deviceStore.IsRegistered(MacAddress));
        }

        [Fact]
        public void IsRegistered_AfterMarkRegistered_ReturnsTrue()
        {
            _deviceStore.MarkRegistered(MacAddress);

            Assert.True(_deviceStore.IsRegistered(MacAddress));
        }

        [Fact]
        public void TryGetPolicies_NothingCached_ReturnsFalse()
        {
            Assert.False(_deviceStore.TryGetPolicies(MacAddress, out _));
        }

        [Fact]
        public void TryGetPolicies_AfterSetPolicies_ReturnsPolicies()
        {
            List<Policy> policies = [TestData.Policy()];
            _deviceStore.SetPolicies(MacAddress, policies);

            _deviceStore.TryGetPolicies(MacAddress, out List<Policy>? cached);

            Assert.Same(policies, cached);
        }

        [Fact]
        public void ForgetPolicies_AfterSetPolicies_RemovesCache()
        {
            _deviceStore.SetPolicies(MacAddress, [TestData.Policy()]);

            _deviceStore.ForgetPolicies(MacAddress);

            Assert.False(_deviceStore.TryGetPolicies(MacAddress, out _));
        }

        [Fact]
        public void TryGetOwner_AfterSetOwner_ReturnsAccount()
        {
            Guid accountId = Guid.NewGuid();
            _deviceStore.SetOwner(MacAddress, accountId);

            _deviceStore.TryGetOwner(MacAddress, out Guid cached);

            Assert.Equal(accountId, cached);
        }

        [Fact]
        public void ForgetOwner_AfterSetOwner_RemovesCache()
        {
            _deviceStore.SetOwner(MacAddress, Guid.NewGuid());

            _deviceStore.ForgetOwner(MacAddress);

            Assert.False(_deviceStore.TryGetOwner(MacAddress, out _));
        }

        [Fact]
        public void GetActuatorState_UnknownDevice_ReturnsOff()
        {
            Assert.Equal(ActuatorState.Off, _deviceStore.GetActuatorState(TestData.ActuatorMacAddress));
        }

        [Fact]
        public async Task SetActuatorState_DifferentState_ReturnsTrue()
        {
            _deviceStore.GetActuatorState(TestData.ActuatorMacAddress);

            bool hasChanged = await _deviceStore.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, CancellationToken.None);

            Assert.True(hasChanged);
        }

        [Fact]
        public async Task SetActuatorState_SameState_ReturnsFalse()
        {
            _deviceStore.GetActuatorState(TestData.ActuatorMacAddress);

            bool hasChanged = await _deviceStore.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.Off, CancellationToken.None);

            Assert.False(hasChanged);
        }

        [Fact]
        public async Task SetActuatorState_NewState_IsReturnedByGetActuatorState()
        {
            await _deviceStore.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, CancellationToken.None);

            Assert.Equal(ActuatorState.On, _deviceStore.GetActuatorState(TestData.ActuatorMacAddress));
        }

        [Fact]
        public void RecordTelemetry_FirstReading_ReturnsTrue()
        {
            Assert.True(_deviceStore.RecordTelemetry(MacAddress, LightReading(40)));
        }

        [Fact]
        public void RecordTelemetry_SameReading_ReturnsFalse()
        {
            _deviceStore.RecordTelemetry(MacAddress, LightReading(40));

            Assert.False(_deviceStore.RecordTelemetry(MacAddress, LightReading(40)));
        }

        [Fact]
        public void RecordTelemetry_ChangedReading_ReturnsTrue()
        {
            _deviceStore.RecordTelemetry(MacAddress, LightReading(40));

            Assert.True(_deviceStore.RecordTelemetry(MacAddress, LightReading(41)));
        }

        [Fact]
        public void TryGetTelemetry_AfterRecordTelemetry_ReturnsLatestReading()
        {
            _deviceStore.RecordTelemetry(MacAddress, LightReading(40));
            _deviceStore.RecordTelemetry(MacAddress, LightReading(41));

            _deviceStore.TryGetTelemetry(MacAddress, out BaseTelemetry? telemetry);

            Assert.Equal(41, Assert.IsType<LightTelemetry>(telemetry).LightReading);
        }

        [Fact]
        public void GetMacAddresses_AfterRecordTelemetry_ContainsDevice()
        {
            _deviceStore.RecordTelemetry(MacAddress, LightReading(40));

            Assert.Contains(MacAddress, _deviceStore.GetMacAddresses());
        }

        /// <summary>
        /// Create a light reading for test sensor
        /// </summary>
        private static LightTelemetry LightReading(int lightReading)
        {
            return new LightTelemetry { MacAddress = MacAddress, DeviceType = DeviceType.LightSensor, LightReading = lightReading };
        }
    }
}

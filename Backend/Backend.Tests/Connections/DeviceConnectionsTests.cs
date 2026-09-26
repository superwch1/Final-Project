using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Backend.Tests.Connections
{
    public class DeviceConnectionsTests
    {
        private const string MacAddress = TestData.SensorMacAddress;
        private const string LightData = "{\"deviceType\":\"LightSensor\",\"lightReading\":42}";

        private readonly DeviceConnections _deviceConnections = new(Options.Create(TestOptions.Device), new DeviceKeyService());
        private readonly List<(string MacAddress, BaseTelemetry Telemetry)> _received = [];

        public DeviceConnectionsTests()
        {
            _deviceConnections.SubscribeToTelemetryReceived(eventArgs =>
            {
                _received.Add(eventArgs);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public void ServerTimeMessage_Called_ContainsCurrentUnixTime()
        {
            long before = TestOptions.Now();

            using JsonDocument document = JsonDocument.Parse(DeviceConnections.ServerTimeMessage());
            long serverTime = document.RootElement.GetProperty("serverTime").GetInt64();

            Assert.InRange(serverTime, before, TestOptions.Now());
        }

        [Fact]
        public async Task DeviceEcho_InitialMessages_SentToDevice()
        {
            FakeWebSocket webSocket = new();

            await _deviceConnections.DeviceEcho(webSocket, MacAddress, DeviceType.LightSensor, ["first", "second"], CancellationToken.None);

            Assert.Equal(["first", "second"], webSocket.Sent);
        }

        [Fact]
        public async Task DeviceEcho_ValidSignedMessage_RaisesTelemetry()
        {
            await EchoAsync(TestOptions.SignedMessage(MacAddress, DeviceType.LightSensor, TestOptions.Now(), LightData));

            Assert.Equal(42, Assert.IsType<LightTelemetry>(Assert.Single(_received).Telemetry).LightReading);
        }

        [Fact]
        public async Task DeviceEcho_ValidSignedMessage_RaisesTelemetryForDevice()
        {
            await EchoAsync(TestOptions.SignedMessage(MacAddress, DeviceType.LightSensor, TestOptions.Now(), LightData));

            Assert.Equal(MacAddress, Assert.Single(_received).MacAddress);
        }

        [Fact]
        public async Task DeviceEcho_WrongMasterKey_IgnoresMessage()
        {
            await EchoAsync(TestOptions.SignedMessage(MacAddress, DeviceType.LightSensor, TestOptions.Now(), LightData, "wrong-master-key"));

            Assert.Empty(_received);
        }

        [Fact]
        public async Task DeviceEcho_SignedForAnotherDeviceType_IgnoresMessage()
        {
            await EchoAsync(TestOptions.SignedMessage(MacAddress, DeviceType.TempAndHumidSensor, TestOptions.Now(), LightData));

            Assert.Empty(_received);
        }

        [Fact]
        public async Task DeviceEcho_TimestampOutsideClockSkew_IgnoresMessage()
        {
            long stale = TestOptions.Now() - (long)TestOptions.Device.MaxClockSkew.TotalMilliseconds - 1000;

            await EchoAsync(TestOptions.SignedMessage(MacAddress, DeviceType.LightSensor, stale, LightData));

            Assert.Empty(_received);
        }

        [Fact]
        public async Task DeviceEcho_ReplayedMessage_RaisesTelemetryOnce()
        {
            string message = TestOptions.SignedMessage(MacAddress, DeviceType.LightSensor, TestOptions.Now(), LightData);

            await EchoAsync(message, message);

            Assert.Single(_received);
        }

        [Fact]
        public async Task DeviceEcho_InvalidJson_IgnoresMessage()
        {
            await EchoAsync("not json");

            Assert.Empty(_received);
        }

        [Fact]
        public async Task NotifyActuatorState_ConnectedDevice_SendsState()
        {
            FakeWebSocket webSocket = new(stayOpen: true);
            Task echo = _deviceConnections.DeviceEcho(webSocket, TestData.ActuatorMacAddress, DeviceType.LedActuator, ["connected"], CancellationToken.None);
            await Wait.UntilAsync(() => webSocket.Sent.Contains("connected"));

            await _deviceConnections.NotifyActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, CancellationToken.None);
            webSocket.RequestClose();
            await echo;

            Assert.Contains("On", webSocket.Sent);
        }

        [Fact]
        public async Task NotifyActuatorState_NoConnection_DoesNotThrow()
        {
            Exception? exception = await Record.ExceptionAsync(() =>
                _deviceConnections.NotifyActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task BroadcastServerTime_ConnectedDevice_SendsServerTime()
        {
            FakeWebSocket webSocket = new(stayOpen: true);
            Task echo = _deviceConnections.DeviceEcho(webSocket, MacAddress, DeviceType.LightSensor, ["connected"], CancellationToken.None);
            await Wait.UntilAsync(() => webSocket.Sent.Contains("connected"));

            await _deviceConnections.BroadcastServerTime(CancellationToken.None);
            webSocket.RequestClose();
            await echo;

            Assert.Contains(webSocket.Sent, message => message.Contains("serverTime"));
        }

        /// <summary>
        /// Connect the test sensor and send the messages
        /// </summary>
        private Task EchoAsync(params string[] messages)
        {
            return _deviceConnections.DeviceEcho(new FakeWebSocket(messages), MacAddress, DeviceType.LightSensor, [], CancellationToken.None);
        }
    }
}

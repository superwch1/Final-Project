using Backend.Connections;
using Backend.Enumerations;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Services
{
    public class ServerTimeSyncBackgroundServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_ConnectedDevice_ReceivesServerTime()
        {
            DeviceConnections deviceConnections = new(Options.Create(TestOptions.Device), new DeviceKeyService());
            ServerTimeSyncBackgroundService service = new(deviceConnections, Options.Create(TestOptions.Device));
            FakeWebSocket webSocket = new(stayOpen: true);
            Task echo = deviceConnections.DeviceEcho(webSocket, TestData.SensorMacAddress, DeviceType.LightSensor, [], CancellationToken.None);

            await service.StartAsync(CancellationToken.None);
            await Wait.UntilAsync(() => webSocket.Sent.Any(message => message.Contains("serverTime")));
            await service.StopAsync(CancellationToken.None);
            webSocket.RequestClose();
            await echo;

            Assert.Contains(webSocket.Sent, message => message.Contains("serverTime"));
        }
    }
}

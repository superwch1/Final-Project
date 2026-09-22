using Backend.Connections;
using Backend.Models;
using Microsoft.Extensions.Options;

namespace Backend
{
    /// <summary>
    /// Resends the server clock to connected devices.
    /// </summary>
    public sealed class ServerTimeSync : BackgroundService
    {
        private readonly DeviceConnections _deviceConnections;
        private readonly DeviceOptions _options;

        public ServerTimeSync(DeviceConnections deviceConnections, IOptions<DeviceOptions> options)
        {
            _deviceConnections = deviceConnections;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(_options.ClockSyncInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _deviceConnections.BroadcastServerTime(stoppingToken);
                }
                catch
                {
                    // Exception for time sync
                }
            }
        }
    }
}

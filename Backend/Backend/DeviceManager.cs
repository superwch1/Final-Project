using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Backend
{
    public class DeviceManager
    {
        private readonly ConcurrentDictionary<string, int> _sensorReadings = new();

        private readonly ConcurrentDictionary<string, bool> _actuatorStates = new();
        private readonly ConcurrentDictionary<string, Channel<bool>> _actuatorChannels = new();

        public void UpdateSensorReading(string macAddress, int reading)
        {
            _sensorReadings.AddOrUpdate(macAddress, reading, (_, _) => reading);
        }

        public void NotifyActuatorState(string macAddress, bool turnedOn)
        {
            bool changed = true;
            _actuatorStates.AddOrUpdate(macAddress, turnedOn, (_, previousValue) =>
            {
                changed = previousValue != turnedOn;
                return turnedOn;
            });
            
            if (changed)
            {
                GetOrAddActuatorChannel(macAddress).Writer.TryWrite(turnedOn);
            } 
        }

        public bool GetDeviceState(string macAddress) => _actuatorStates.GetOrAdd(macAddress, false);

        public async Task<bool> WaitForDeviceStateChangeAsync(string macAddress, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(timeout);

            try 
            { 
                return await GetOrAddActuatorChannel(macAddress).Reader.ReadAsync(cancellationTokenSource.Token); 
            }
            catch (OperationCanceledException) 
            {
                return _actuatorStates.GetOrAdd(macAddress, false);
            }
        }


        // only keep the latest command
        private Channel<bool> GetOrAddActuatorChannel(string deviceId) =>
            _actuatorChannels.GetOrAdd(deviceId, _ => Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest }));
    }
}

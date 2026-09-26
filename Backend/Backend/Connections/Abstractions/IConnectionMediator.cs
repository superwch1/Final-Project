using Backend.Enumerations;
using System.Net.WebSockets;

namespace Backend.Connections
{
    public interface IConnectionMediator
    {
        /// <summary>
        /// Stores the actuator state and sends it to the device if it changed
        /// </summary>
        Task SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server time and actuator state to a new device
        /// </summary>
        Task DeviceEcho(WebSocket webSocket, string macAddress, DeviceType deviceType, CancellationToken cancellationToken);

        /// <summary>
        /// Runs a dashboard connection until it closes
        /// </summary>
        Task DashboardEcho(WebSocket webSocket, CancellationToken cancellationToken);
    }
}

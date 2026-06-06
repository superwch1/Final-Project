using Backend.Connections;
using Backend.Enumerations;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Backend.Controllers
{
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly DeviceConnections _deviceConnections;

        public DeviceController(DeviceConnections deviceConnections)
        {
            _deviceConnections = deviceConnections;
        }


        [HttpGet("ws")]
        public async Task WebSocket([FromQuery] string macAddress, [FromQuery] DeviceType deviceType, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connected - {macAddress}");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _deviceConnections.DeviceEcho(webSocket, macAddress, deviceType, cancellationToken);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}

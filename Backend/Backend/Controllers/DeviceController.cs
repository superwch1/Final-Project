using Backend.Enumerations;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Backend.Controllers
{
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ConnectionsManager _connectionsManager;

        public DeviceController(ConnectionsManager connectionsManager)
        {
            _connectionsManager = connectionsManager;
        }


        [HttpGet("ws")]
        public async Task WebSocket([FromQuery] string macAddress, [FromQuery] DeviceType deviceType, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connected - {macAddress}");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _connectionsManager.DeviceEcho(webSocket, macAddress, deviceType, cancellationToken);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }


        [HttpGet("{macAddress}/{actuatorState}")]
        public async Task<ActionResult> UpdateActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            await _connectionsManager.SetActuatorState(macAddress, actuatorState, cancellationToken);
            return Ok($"Message sent to device {macAddress}");
        }
    }
}

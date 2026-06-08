using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ConnectionMediator _connectionMediator;

        public DeviceController(ConnectionMediator connectionMediator)
        {
            _connectionMediator = connectionMediator;
        }


        [HttpGet("ws")]
        public async Task WebSocket([FromQuery] string macAddress, [FromQuery] DeviceType deviceType, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connected - {macAddress}");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _connectionMediator.DeviceEcho(webSocket, macAddress, deviceType, cancellationToken);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }


        [HttpPost("actuator/state")]
        public async Task<ActionResult> SetActuatorState(SetActuatorStateRequest request, CancellationToken cancellationToken)
        {
            await _connectionMediator.SetActuatorState(request.MacAddress, request.ActuatorState, cancellationToken);
            return Ok($"Message sent to device {request.MacAddress}");
        }
    }
}

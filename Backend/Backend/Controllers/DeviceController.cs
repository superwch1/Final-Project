using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ConnectionMediator _connectionMediator;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IRoomRepository _roomRepository;

        public DeviceController(ConnectionMediator connectionMediator, IDeviceRepository deviceRepository, IRoomRepository roomRepository)
        {
            _connectionMediator = connectionMediator;
            _deviceRepository = deviceRepository;
            _roomRepository = roomRepository;
        }


        [HttpGet("ws")]
        [AllowAnonymous]
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
        [Authorize]
        public async Task<ActionResult> SetActuatorState(SetActuatorStateRequest request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid accountId))
            {
                return Unauthorized();
            }

            string macAddress = DeviceKey.NormalizeMacAddress(request.MacAddress);
            Device? device = await _deviceRepository.FindByMacAddressAsync(macAddress, cancellationToken);
            if (device is null)
            {
                return NotFound();
            }

            if (device.RoomId is null)
            {
                return NotFound();
            }

            Room? room = await _roomRepository.FindByIdAsync(device.RoomId.Value, cancellationToken);
            if (room?.AccountId != accountId)
            {
                return NotFound();
            }

            await _connectionMediator.SetActuatorState(macAddress, request.ActuatorState, cancellationToken);

            return Ok();
        }
    }
}

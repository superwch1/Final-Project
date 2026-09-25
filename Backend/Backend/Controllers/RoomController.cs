using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly DeviceStore _deviceStore;

        public RoomController(IRoomRepository roomRepository, IDeviceRepository deviceRepository, IPolicyRepository policyRepository, DeviceStore deviceStore)
        {
            _roomRepository = roomRepository;
            _deviceRepository = deviceRepository;
            _policyRepository = policyRepository;
            _deviceStore = deviceStore;
        }


        /// <summary>
        /// List the signed-in account's rooms and devices.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomResponse>>> GetAll(CancellationToken cancellationToken)
        {
            if (!TryGetAccountId(out Guid accountId))
            {
                return Unauthorized();
            }

            List<Room> rooms = await _roomRepository.FindByAccountAsync(accountId, cancellationToken);

            return Ok(rooms.Select(RoomResponse.FromRoom));
        }


        /// <summary>
        /// Create a room.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RoomResponse>> Create(CreateRoomRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetAccountId(out Guid accountId))
            {
                return Unauthorized();
            }

            Room room = new()
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Name = request.Name.Trim()
            };

            await _roomRepository.AddAsync(room, cancellationToken);

            return Ok(RoomResponse.FromRoom(room));
        }


        /// <summary>
        /// Rename a room.
        /// </summary>
        [HttpPut("{roomId}")]
        public async Task<ActionResult<RoomResponse>> Update(Guid roomId, UpdateRoomRequest request, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);

            if (room is null)
            {
                return NotFound();
            }

            room.Name = request.Name.Trim();
            await _roomRepository.UpdateAsync(room, cancellationToken);

            return Ok(RoomResponse.FromRoom(room));
        }


        /// <summary>
        /// Delete a room and unpair their devices.
        /// </summary>
        [HttpDelete("{roomId}")]
        public async Task<ActionResult> Delete(Guid roomId, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);

            if (room is null)
            {
                return NotFound();
            }

            foreach (Device device in room.Devices)
            {
                _deviceStore.ForgetOwner(device.MacAddress);
            }

            await _roomRepository.DeleteAsync(room, cancellationToken);

            return NoContent();
        }


        /// <summary>
        /// Pair a device into a room.
        /// </summary>
        [HttpPost("{roomId}/device")]
        public async Task<ActionResult<DeviceResponse>> PairDevice(Guid roomId, PairDeviceRequest request, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);
            if (room is null)
            {
                return NotFound();
            }

            string macAddress = DeviceKey.NormalizeMacAddress(request.MacAddress);
            if (macAddress.Length != 12)
            {
                return BadRequest("That is not a MAC address.");
            }

            Device? device = await _deviceRepository.FindByMacAddressAsync(macAddress, cancellationToken);
            if (device is null)
            {
                return NotFound("No device with that address has reported yet.");
            }

            if (device.RoomId is not null)
            {
                return Conflict("That device is already paired.");
            }

            device.RoomId = roomId;
            device.Name = request.Name.Trim();

            await _deviceRepository.UpdateAsync(device, cancellationToken);
            _deviceStore.SetOwner(macAddress, room.AccountId);

            return Ok(DeviceResponse.FromDevice(device));
        }


        /// <summary>
        /// Unpair a device.
        /// </summary>
        [HttpDelete("{roomId}/device/{macAddress}")]
        public async Task<ActionResult> UnpairDevice(Guid roomId, string macAddress, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);
            if (room is null)
            {
                return NotFound();
            }

            Device? device = await _deviceRepository.FindByMacAddressAsync(DeviceKey.NormalizeMacAddress(macAddress), cancellationToken);
            if (device is null || device.RoomId != roomId)
            {
                return NotFound();
            }

            await _policyRepository.DeleteByDeviceAsync(device.MacAddress, cancellationToken);

            device.RoomId = null;
            await _deviceRepository.UpdateAsync(device, cancellationToken);

            _deviceStore.ForgetOwner(device.MacAddress);
            _deviceStore.ForgetPolicies(device.MacAddress);

            return NoContent();
        }


        /// <summary>
        /// Find a room, or null when it does not exist or belongs to someone else.
        /// </summary>
        private async Task<Room?> FindRoomIfOwnedAsync(Guid roomId, CancellationToken cancellationToken)
        {
            if (!TryGetAccountId(out Guid accountId))
            {
                return null;
            }

            Room? room = await _roomRepository.FindByIdAsync(roomId, cancellationToken);
            return (room?.AccountId == accountId) ? room : null;
        }

        private bool TryGetAccountId(out Guid accountId)
        {
            return Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out accountId);
        }
    }
}

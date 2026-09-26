using Backend.Connections;
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
        private readonly IDeviceStore _deviceStore;
        private readonly IDeviceKeyService _deviceKeyService;

        public RoomController(IRoomRepository roomRepository, IDeviceRepository deviceRepository, IPolicyRepository policyRepository, IDeviceStore deviceStore, IDeviceKeyService deviceKeyService)
        {
            _roomRepository = roomRepository;
            _deviceRepository = deviceRepository;
            _policyRepository = policyRepository;
            _deviceStore = deviceStore;
            _deviceKeyService = deviceKeyService;
        }



        /// <summary>
        /// Returns every room owned by the account
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
        /// Creates a room 
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
        /// Renames a room
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
        /// Deletes a room
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
        /// Pairs a device that has already in a room
        /// </summary>
        [HttpPost("{roomId}/device")]
        public async Task<ActionResult<DeviceResponse>> PairDevice(Guid roomId, PairDeviceRequest request, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);
            if (room is null)
            {
                return NotFound();
            }

            string macAddress = _deviceKeyService.NormalizeMacAddress(request.MacAddress);
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
        /// Removes a device from a room and deletes its policies
        /// </summary>
        [HttpDelete("{roomId}/device/{macAddress}")]
        public async Task<ActionResult> UnpairDevice(Guid roomId, string macAddress, CancellationToken cancellationToken)
        {
            Room? room = await FindRoomIfOwnedAsync(roomId, cancellationToken);
            if (room is null)
            {
                return NotFound();
            }

            Device? device = await _deviceRepository.FindByMacAddressAsync(_deviceKeyService.NormalizeMacAddress(macAddress), cancellationToken);
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
        /// Loads a room only if it is owned by the account
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

        /// <summary>
        /// Reads the account ID from the user's claims
        /// </summary>
        private bool TryGetAccountId(out Guid accountId)
        {
            return Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out accountId);
        }
    }
}

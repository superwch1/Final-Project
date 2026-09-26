using Backend.Connections;
using Backend.Controllers;
using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Backend.Tests.Controllers
{
    public class RoomControllerTests
    {
        private readonly Mock<IRoomRepository> _roomRepository = new();
        private readonly Mock<IDeviceRepository> _deviceRepository = new();
        private readonly Mock<IPolicyRepository> _policyRepository = new();
        private readonly Mock<IDeviceStore> _deviceStore = new();
        private readonly Guid _accountId = Guid.NewGuid();
        private readonly Room _room;

        public RoomControllerTests()
        {
            _room = TestData.Room(_accountId);
            _roomRepository.Setup(x => x.FindByIdAsync(_room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
        }

        [Fact]
        public async Task GetAll_SignedOut_ReturnsUnauthorized()
        {
            ActionResult<IEnumerable<RoomResponse>> result = await CreateController(null).GetAll(CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_SignedIn_ReturnsAccountRooms()
        {
            _roomRepository.Setup(x => x.FindByAccountAsync(_accountId, It.IsAny<CancellationToken>())).ReturnsAsync([_room]);

            ActionResult<IEnumerable<RoomResponse>> result = await CreateController(_accountId).GetAll(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<RoomResponse>>(ok.Value));
        }

        [Fact]
        public async Task Create_SignedIn_SavesTrimmedName()
        {
            await CreateController(_accountId).Create(new CreateRoomRequest { Name = "  Kitchen " }, CancellationToken.None);

            _roomRepository.Verify(x => x.AddAsync(It.Is<Room>(r => r.Name == "Kitchen"), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Create_SignedIn_AssignsRoomToAccount()
        {
            await CreateController(_accountId).Create(new CreateRoomRequest { Name = "Kitchen" }, CancellationToken.None);

            _roomRepository.Verify(x => x.AddAsync(It.Is<Room>(r => r.AccountId == _accountId), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Update_RoomOfAnotherAccount_ReturnsNotFound()
        {
            ActionResult<RoomResponse> result = await CreateController(Guid.NewGuid()).Update(_room.Id, new UpdateRoomRequest { Name = "Kitchen" }, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Update_OwnedRoom_RenamesRoom()
        {
            await CreateController(_accountId).Update(_room.Id, new UpdateRoomRequest { Name = "Kitchen" }, CancellationToken.None);

            Assert.Equal("Kitchen", _room.Name);
        }

        [Fact]
        public async Task Delete_UnknownRoom_ReturnsNotFound()
        {
            ActionResult result = await CreateController(_accountId).Delete(Guid.NewGuid(), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_RoomWithDevice_ForgetsDeviceOwner()
        {
            _room.Devices.Add(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, _room.Id));

            await CreateController(_accountId).Delete(_room.Id, CancellationToken.None);

            _deviceStore.Verify(x => x.ForgetOwner(TestData.SensorMacAddress));
        }

        [Fact]
        public async Task PairDevice_InvalidMacAddress_ReturnsBadRequest()
        {
            ActionResult<DeviceResponse> result = await CreateController(_accountId).PairDevice(_room.Id, PairRequest("AABB"), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PairDevice_DeviceNeverReported_ReturnsNotFound()
        {
            ActionResult<DeviceResponse> result = await CreateController(_accountId).PairDevice(_room.Id, PairRequest(TestData.SensorMacAddress), CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task PairDevice_AlreadyPaired_ReturnsConflict()
        {
            SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, Guid.NewGuid()));

            ActionResult<DeviceResponse> result = await CreateController(_accountId).PairDevice(_room.Id, PairRequest(TestData.SensorMacAddress), CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task PairDevice_UnpairedDevice_PairsToRoom()
        {
            Device device = SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor));

            await CreateController(_accountId).PairDevice(_room.Id, PairRequest(TestData.SensorMacAddress), CancellationToken.None);

            Assert.Equal(_room.Id, device.RoomId);
        }

        [Fact]
        public async Task PairDevice_UnpairedDevice_CachesOwner()
        {
            SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor));

            await CreateController(_accountId).PairDevice(_room.Id, PairRequest(TestData.SensorMacAddress), CancellationToken.None);

            _deviceStore.Verify(x => x.SetOwner(TestData.SensorMacAddress, _accountId));
        }

        [Fact]
        public async Task UnpairDevice_DeviceInAnotherRoom_ReturnsNotFound()
        {
            SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, Guid.NewGuid()));

            ActionResult result = await CreateController(_accountId).UnpairDevice(_room.Id, TestData.SensorMacAddress, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UnpairDevice_PairedDevice_DeletesItsPolicies()
        {
            SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, _room.Id));

            await CreateController(_accountId).UnpairDevice(_room.Id, TestData.SensorMacAddress, CancellationToken.None);

            _policyRepository.Verify(x => x.DeleteByDeviceAsync(TestData.SensorMacAddress, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task UnpairDevice_PairedDevice_ClearsRoom()
        {
            Device device = SetUpDevice(TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, _room.Id));

            await CreateController(_accountId).UnpairDevice(_room.Id, TestData.SensorMacAddress, CancellationToken.None);

            Assert.Null(device.RoomId);
        }

        /// <summary>
        /// Creates the controller and signed in as the account
        /// </summary>
        private RoomController CreateController(Guid? accountId)
        {
            return new RoomController(_roomRepository.Object, _deviceRepository.Object, _policyRepository.Object, _deviceStore.Object, new DeviceKeyService())
                .SignedInAs(accountId);
        }

        /// <summary>
        /// Make the device findable by its MAC address
        /// </summary>
        private Device SetUpDevice(Device device)
        {
            _deviceRepository.Setup(x => x.FindByMacAddressAsync(device.MacAddress, It.IsAny<CancellationToken>())).ReturnsAsync(device);
            return device;
        }

        /// <summary>
        /// Build a request to pair the device
        /// </summary>
        private static PairDeviceRequest PairRequest(string macAddress)
        {
            return new PairDeviceRequest { MacAddress = macAddress, Name = "Desk lamp" };
        }
    }
}

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
    public class DeviceControllerTests
    {
        private readonly Mock<IConnectionMediator> _connectionMediator = new();
        private readonly Mock<IDeviceRepository> _deviceRepository = new();
        private readonly Mock<IRoomRepository> _roomRepository = new();
        private readonly Guid _accountId = Guid.NewGuid();

        [Fact]
        public async Task SetActuatorState_SignedOut_ReturnsUnauthorized()
        {
            ActionResult result = await CreateController(null).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SetActuatorState_UnknownDevice_ReturnsNotFound()
        {
            ActionResult result = await CreateController(_accountId).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetActuatorState_UnpairedDevice_ReturnsNotFound()
        {
            SetUpDevice(TestData.Device(TestData.ActuatorMacAddress, DeviceType.LedActuator));

            ActionResult result = await CreateController(_accountId).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetActuatorState_DeviceInAnotherAccountsRoom_ReturnsNotFound()
        {
            SetUpPairedDevice(Guid.NewGuid());

            ActionResult result = await CreateController(_accountId).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetActuatorState_OwnedDevice_ReturnsOk()
        {
            SetUpPairedDevice(_accountId);

            ActionResult result = await CreateController(_accountId).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task SetActuatorState_OwnedDevice_SwitchesActuator()
        {
            SetUpPairedDevice(_accountId);

            await CreateController(_accountId).SetActuatorState(SwitchOnRequest(TestData.ActuatorMacAddress), CancellationToken.None);

            _connectionMediator.Verify(x => x.SetActuatorState(TestData.ActuatorMacAddress, ActuatorState.On, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task SetActuatorState_MacAddressWithColons_LooksUpNormalisedAddress()
        {
            await CreateController(_accountId).SetActuatorState(SwitchOnRequest("aa:bb:cc:dd:ee:02"), CancellationToken.None);

            _deviceRepository.Verify(x => x.FindByMacAddressAsync(TestData.ActuatorMacAddress, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WebSocket_PlainHttpRequest_Returns400()
        {
            DeviceController controller = CreateController(null);

            await controller.WebSocket(TestData.SensorMacAddress, DeviceType.LightSensor, CancellationToken.None);

            Assert.Equal(400, controller.HttpContext.Response.StatusCode);
        }

        /// <summary>
        /// Create the controller and signed in as the account
        /// </summary>
        private DeviceController CreateController(Guid? accountId)
        {
            return new DeviceController(_connectionMediator.Object, _deviceRepository.Object, _roomRepository.Object, new DeviceKeyService())
                .SignedInAs(accountId);
        }

        /// <summary>
        /// Make the device findable by its MAC address
        /// </summary>
        private void SetUpDevice(Device device)
        {
            _deviceRepository.Setup(x => x.FindByMacAddressAsync(device.MacAddress, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        }

        /// <summary>
        /// Pair the test actuator to a room
        /// </summary>
        private void SetUpPairedDevice(Guid ownerAccountId)
        {
            Room room = TestData.Room(ownerAccountId);
            SetUpDevice(TestData.Device(TestData.ActuatorMacAddress, DeviceType.LedActuator, room.Id));
            _roomRepository.Setup(x => x.FindByIdAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        }

        /// <summary>
        /// Build a request to switch the actuator on
        /// </summary>
        private static SetActuatorStateRequest SwitchOnRequest(string macAddress)
        {
            return new SetActuatorStateRequest { MacAddress = macAddress, ActuatorState = ActuatorState.On };
        }
    }
}

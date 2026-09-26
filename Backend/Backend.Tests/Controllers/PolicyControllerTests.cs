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
    public class PolicyControllerTests
    {
        private readonly Mock<IPolicyRepository> _policyRepository = new();
        private readonly Mock<IDeviceRepository> _deviceRepository = new();
        private readonly Mock<IRoomRepository> _roomRepository = new();
        private readonly Mock<IDeviceStore> _deviceStore = new();
        private readonly Guid _accountId = Guid.NewGuid();
        private readonly Room _room;

        public PolicyControllerTests()
        {
            _room = TestData.Room(_accountId);
            _roomRepository.Setup(x => x.FindByIdAsync(_room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
            _policyRepository.Setup(x => x.TryAddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        [Fact]
        public async Task GetAll_SignedOut_ReturnsUnauthorized()
        {
            ActionResult<IEnumerable<PolicyResponse>> result = await CreateController(null).GetAll(CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_SignedIn_ReturnsAccountPolicies()
        {
            _policyRepository.Setup(x => x.FindByAccountAsync(_accountId, It.IsAny<CancellationToken>())).ReturnsAsync([TestData.Policy()]);

            ActionResult<IEnumerable<PolicyResponse>> result = await CreateController(_accountId).GetAll(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value));
        }

        [Fact]
        public async Task Create_SameSensorAndActuator_ReturnsBadRequest()
        {
            CreatePolicyRequest request = CreateRequest(SensorReading.Light) with { ActuatorMacAddress = TestData.SensorMacAddress };

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(request, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_DeviceNotOwned_ReturnsNotFound()
        {
            OwnDevice(TestData.SensorMacAddress, DeviceType.LightSensor);

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_SensorIsActuator_ReturnsBadRequest()
        {
            OwnDevice(TestData.SensorMacAddress, DeviceType.FanActuator);
            OwnDevice(TestData.ActuatorMacAddress, DeviceType.LedActuator);

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_ActuatorIsSensor_ReturnsBadRequest()
        {
            OwnDevice(TestData.SensorMacAddress, DeviceType.LightSensor);
            OwnDevice(TestData.ActuatorMacAddress, DeviceType.TempAndHumidSensor);

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_ReadingNotReportedBySensor_ReturnsBadRequest()
        {
            OwnSensorAndActuator();

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Temperature), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_ActuatorAlreadyDriven_ReturnsConflict()
        {
            OwnSensorAndActuator();
            _policyRepository.Setup(x => x.TryAddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_ValidPolicy_ReturnsEnabledPolicy()
        {
            OwnSensorAndActuator();

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.True(Assert.IsType<PolicyResponse>(ok.Value).IsEnabled);
        }

        [Fact]
        public async Task Create_ValidPolicy_ClearsCachedSensorPolicies()
        {
            OwnSensorAndActuator();

            await CreateController(_accountId).Create(CreateRequest(SensorReading.Light), CancellationToken.None);

            _deviceStore.Verify(x => x.ForgetPolicies(TestData.SensorMacAddress));
        }

        [Fact]
        public async Task Update_PolicyNotFound_ReturnsNotFound()
        {
            ActionResult<PolicyResponse> result = await CreateController(_accountId).Update(Guid.NewGuid(), UpdateRequest(SensorReading.Light), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Update_ReadingNotReportedBySensor_ReturnsBadRequest()
        {
            Policy policy = SetUpOwnedPolicy();

            ActionResult<PolicyResponse> result = await CreateController(_accountId).Update(policy.Id, UpdateRequest(SensorReading.Humidity), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Update_Disable_SavesDisabledPolicy()
        {
            Policy policy = SetUpOwnedPolicy();

            await CreateController(_accountId).Update(policy.Id, UpdateRequest(SensorReading.Light) with { IsEnabled = false }, CancellationToken.None);

            _policyRepository.Verify(x => x.UpdateAsync(It.Is<Policy>(p => !p.IsEnabled), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Delete_PolicyNotFound_ReturnsNotFound()
        {
            ActionResult result = await CreateController(_accountId).Delete(Guid.NewGuid(), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_OwnedPolicy_DeletesPolicy()
        {
            Policy policy = SetUpOwnedPolicy();

            await CreateController(_accountId).Delete(policy.Id, CancellationToken.None);

            _policyRepository.Verify(x => x.DeleteAsync(policy, It.IsAny<CancellationToken>()));
        }

        /// <summary>
        /// Creates the controller and signed in as the account
        /// </summary>
        private PolicyController CreateController(Guid? accountId)
        {
            return new PolicyController(_policyRepository.Object, _deviceRepository.Object, _roomRepository.Object, _deviceStore.Object, new DeviceKeyService())
                .SignedInAs(accountId);
        }

        /// <summary>
        /// Pair a device of the given type to the account room
        /// </summary>
        private Device OwnDevice(string macAddress, DeviceType deviceType)
        {
            Device device = TestData.Device(macAddress, deviceType, _room.Id);
            _deviceRepository.Setup(x => x.FindByMacAddressAsync(macAddress, It.IsAny<CancellationToken>())).ReturnsAsync(device);

            return device;
        }

        /// <summary>
        /// Pair a light sensor and an LED to the account room
        /// </summary>
        private void OwnSensorAndActuator()
        {
            OwnDevice(TestData.SensorMacAddress, DeviceType.LightSensor);
            OwnDevice(TestData.ActuatorMacAddress, DeviceType.LedActuator);
        }

        /// <summary>
        /// Store a light policy whose sensor belongs to the account
        /// </summary>
        private Policy SetUpOwnedPolicy()
        {
            Policy policy = TestData.Policy();
            policy.Sensor = OwnDevice(TestData.SensorMacAddress, DeviceType.LightSensor);
            _policyRepository.Setup(x => x.FindByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

            return policy;
        }

        /// <summary>
        /// Build a request linking the sensor to actuator
        /// </summary>
        private static CreatePolicyRequest CreateRequest(SensorReading reading)
        {
            return new CreatePolicyRequest
            {
                SensorMacAddress = TestData.SensorMacAddress,
                ActuatorMacAddress = TestData.ActuatorMacAddress,
                Reading = reading,
                Comparison = Comparison.Above,
                Threshold = 50,
                ActuatorState = ActuatorState.On
            };
        }

        /// <summary>
        /// Builds an update request that keeps the policy enabled
        /// </summary>
        private static UpdatePolicyRequest UpdateRequest(SensorReading reading)
        {
            return new UpdatePolicyRequest
            {
                Reading = reading,
                Comparison = Comparison.Below,
                Threshold = 20,
                ActuatorState = ActuatorState.Off,
                IsEnabled = true
            };
        }
    }
}

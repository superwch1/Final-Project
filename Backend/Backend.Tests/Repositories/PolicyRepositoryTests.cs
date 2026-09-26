using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Repositories
{
    public sealed class PolicyRepositoryTests : IAsyncLifetime
    {
        private const string OtherActuatorMacAddress = "AABBCCDDEE03";

        private readonly TestDatabase _database = new();
        private readonly PolicyRepository _policyRepository;
        private readonly Account _account = TestData.Account();
        private readonly Room _room;

        public PolicyRepositoryTests()
        {
            _policyRepository = new PolicyRepository(_database.Context);
            _room = TestData.Room(_account.Id);
        }

        /// <summary>
        /// Seeds the account, room and devices
        /// </summary>
        public Task InitializeAsync()
        {
            return _database.SeedAsync(
                _account,
                _room,
                TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, _room.Id),
                TestData.Device(TestData.ActuatorMacAddress, DeviceType.LedActuator, _room.Id),
                TestData.Device(OtherActuatorMacAddress, DeviceType.FanActuator, _room.Id));
        }

        [Fact]
        public async Task TryAddAsync_UndrivenActuator_ReturnsTrue()
        {
            Assert.True(await _policyRepository.TryAddAsync(TestData.Policy(), CancellationToken.None));
        }

        [Fact]
        public async Task TryAddAsync_UndrivenActuator_SavesPolicy()
        {
            Policy policy = TestData.Policy();

            await _policyRepository.TryAddAsync(policy, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.NotNull(await context.Policies.FindAsync(policy.Id));
        }

        [Fact]
        public async Task TryAddAsync_ActuatorAlreadyDriven_ReturnsFalse()
        {
            await _database.SeedAsync(TestData.Policy());

            Assert.False(await _policyRepository.TryAddAsync(TestData.Policy(), CancellationToken.None));
        }

        [Fact]
        public async Task FindByIdAsync_ExistingPolicy_IncludesSensor()
        {
            Policy policy = TestData.Policy();
            await _database.SeedAsync(policy);

            Policy? found = await _policyRepository.FindByIdAsync(policy.Id, CancellationToken.None);

            Assert.Equal(TestData.SensorMacAddress, found?.Sensor?.MacAddress);
        }

        [Fact]
        public async Task FindByIdAsync_ExistingPolicy_IncludesActuator()
        {
            Policy policy = TestData.Policy();
            await _database.SeedAsync(policy);

            Policy? found = await _policyRepository.FindByIdAsync(policy.Id, CancellationToken.None);

            Assert.Equal(TestData.ActuatorMacAddress, found?.Actuator?.MacAddress);
        }

        [Fact]
        public async Task FindBySensorAsync_PoliciesOfTwoSensors_ReturnsOnlySensorPolicies()
        {
            Policy policy = TestData.Policy();
            await _database.SeedAsync(policy, TestData.Policy(TestData.ActuatorMacAddress, OtherActuatorMacAddress));

            List<Policy> policies = await _policyRepository.FindBySensorAsync(TestData.SensorMacAddress, CancellationToken.None);

            Assert.Equal(policy.Id, Assert.Single(policies).Id);
        }

        [Fact]
        public async Task FindByAccountAsync_PolicyInAccountRoom_ReturnsPolicy()
        {
            await _database.SeedAsync(TestData.Policy());

            List<Policy> policies = await _policyRepository.FindByAccountAsync(_account.Id, CancellationToken.None);

            Assert.Single(policies);
        }

        [Fact]
        public async Task FindByAccountAsync_AnotherAccount_ReturnsNothing()
        {
            await _database.SeedAsync(TestData.Policy());

            List<Policy> policies = await _policyRepository.FindByAccountAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.Empty(policies);
        }

        [Fact]
        public async Task UpdateAsync_ChangedThreshold_SavesThreshold()
        {
            Policy policy = TestData.Policy();
            await _database.SeedAsync(policy);
            policy.Threshold = 75;

            await _policyRepository.UpdateAsync(policy, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Equal(75, (await context.Policies.FindAsync(policy.Id))?.Threshold);
        }

        [Fact]
        public async Task DeleteAsync_ExistingPolicy_RemovesPolicy()
        {
            Policy policy = TestData.Policy();
            await _database.SeedAsync(policy);

            await _policyRepository.DeleteAsync(policy, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Null(await context.Policies.FindAsync(policy.Id));
        }

        [Fact]
        public async Task DeleteByDeviceAsync_DeviceIsSensor_RemovesPolicy()
        {
            await _database.SeedAsync(TestData.Policy());

            await _policyRepository.DeleteByDeviceAsync(TestData.SensorMacAddress, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Empty(await context.Policies.ToListAsync());
        }

        [Fact]
        public async Task DeleteByDeviceAsync_DeviceIsActuator_RemovesPolicy()
        {
            await _database.SeedAsync(TestData.Policy());

            await _policyRepository.DeleteByDeviceAsync(TestData.ActuatorMacAddress, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Empty(await context.Policies.ToListAsync());
        }

        [Fact]
        public async Task DeleteByDeviceAsync_UnrelatedDevice_KeepsPolicy()
        {
            await _database.SeedAsync(TestData.Policy());

            await _policyRepository.DeleteByDeviceAsync(OtherActuatorMacAddress, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Single(await context.Policies.ToListAsync());
        }

        public Task DisposeAsync()
        {
            _database.Dispose();
            return Task.CompletedTask;
        }
    }
}

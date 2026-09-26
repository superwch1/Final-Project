using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Tests.Helpers;

namespace Backend.Tests.Repositories
{
    public sealed class DeviceRepositoryTests : IDisposable
    {
        private readonly TestDatabase _database = new();
        private readonly DeviceRepository _deviceRepository;

        public DeviceRepositoryTests()
        {
            _deviceRepository = new DeviceRepository(_database.Context);
        }

        [Fact]
        public async Task TryAddAsync_NewDevice_ReturnsTrue()
        {
            bool isAdded = await _deviceRepository.TryAddAsync(Sensor(), CancellationToken.None);

            Assert.True(isAdded);
        }

        [Fact]
        public async Task TryAddAsync_NewDevice_SavesDevice()
        {
            await _deviceRepository.TryAddAsync(Sensor(), CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.NotNull(await context.Devices.FindAsync(TestData.SensorMacAddress));
        }

        [Fact]
        public async Task TryAddAsync_ExistingMacAddress_ReturnsFalse()
        {
            await _database.SeedAsync(Sensor());

            bool isAdded = await _deviceRepository.TryAddAsync(Sensor(), CancellationToken.None);

            Assert.False(isAdded);
        }

        [Fact]
        public async Task FindByMacAddressAsync_ExistingDevice_ReturnsDevice()
        {
            await _database.SeedAsync(Sensor());

            Device? device = await _deviceRepository.FindByMacAddressAsync(TestData.SensorMacAddress, CancellationToken.None);

            Assert.Equal(DeviceType.LightSensor, device?.DeviceType);
        }

        [Fact]
        public async Task FindByMacAddressAsync_UnknownDevice_ReturnsNull()
        {
            Assert.Null(await _deviceRepository.FindByMacAddressAsync(TestData.SensorMacAddress, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_ChangedName_SavesName()
        {
            Device device = Sensor();
            await _database.SeedAsync(device);
            device.Name = "Desk lamp";

            await _deviceRepository.UpdateAsync(device, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Equal("Desk lamp", (await context.Devices.FindAsync(TestData.SensorMacAddress))?.Name);
        }

        [Fact]
        public async Task DeleteAsync_ExistingDevice_RemovesDevice()
        {
            Device device = Sensor();
            await _database.SeedAsync(device);

            await _deviceRepository.DeleteAsync(device, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Null(await context.Devices.FindAsync(TestData.SensorMacAddress));
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        /// <summary>
        /// Builds the unpaired test light sensor
        /// </summary>
        private static Device Sensor()
        {
            return TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor);
        }
    }
}

using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public sealed class DeviceRepository : IDeviceRepository
    {
        private readonly AppDbContext _dbContext;

        public DeviceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task<Device?> FindByMacAddressAsync(string macAddress, CancellationToken cancellationToken)
        {
            return _dbContext.Devices
                .FirstOrDefaultAsync(x => x.MacAddress == macAddress, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> TryAddAsync(Device device, CancellationToken cancellationToken)
        {
            bool isPaired = await _dbContext.Devices
                .AnyAsync(x => x.MacAddress == device.MacAddress, cancellationToken);

            if (isPaired)
            {
                return false;
            }

            _dbContext.Devices.Add(device);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Device device, CancellationToken cancellationToken)
        {
            _dbContext.Devices.Update(device);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Device device, CancellationToken cancellationToken)
        {
            _dbContext.Devices.Remove(device);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

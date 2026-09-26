using Backend.Models;

namespace Backend.Repositories
{
    public interface IDeviceRepository
    {

        /// <summary>
        /// Finds a device by normalised MAC address, or null if none exists
        /// </summary>
        Task<Device?> FindByMacAddressAsync(string macAddress, CancellationToken cancellationToken);


        /// <summary>
        /// Add a new device, returning false if one with the same MAC address already exists
        /// </summary>
        Task<bool> TryAddAsync(Device device, CancellationToken cancellationToken);


        /// <summary>
        /// Update an existing device
        /// </summary>
        Task UpdateAsync(Device device, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a device
        /// </summary>
        Task DeleteAsync(Device device, CancellationToken cancellationToken);
    }
}

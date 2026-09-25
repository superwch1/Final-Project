using Backend.Models;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for paired devices.
    /// </summary>
    public interface IDeviceRepository
    {
        /// <summary>
        /// Find a device by its normalized MAC address.
        /// </summary>
        Task<Device?> FindByMacAddressAsync(string macAddress, CancellationToken cancellationToken);

        /// <summary>
        /// Pair a device to a room.
        /// </summary>
        Task<bool> TryAddAsync(Device device, CancellationToken cancellationToken);

        /// <summary>
        /// Save changes to a device.
        /// </summary>
        Task UpdateAsync(Device device, CancellationToken cancellationToken);

        /// <summary>
        /// Unpair a device.
        /// </summary>
        Task DeleteAsync(Device device, CancellationToken cancellationToken);
    }
}

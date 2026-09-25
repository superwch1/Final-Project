using Backend.Models;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for automation policies.
    /// </summary>
    public interface IPolicyRepository
    {
        /// <summary>
        /// Find a policy by id.
        /// </summary>
        Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// The policies driven by one sensor.
        /// </summary>
        Task<List<Policy>> FindBySensorAsync(string sensorMacAddress, CancellationToken cancellationToken);

        /// <summary>
        /// Every policy belonging to an account, whichever room its devices are in.
        /// </summary>
        Task<List<Policy>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken);

        /// <summary>
        /// Add a policy, returning false when the actuator already has one.
        /// </summary>
        Task<bool> TryAddAsync(Policy policy, CancellationToken cancellationToken);

        /// <summary>
        /// Save changes to a policy.
        /// </summary>
        Task UpdateAsync(Policy policy, CancellationToken cancellationToken);

        /// <summary>
        /// Remove every policy using this device, as either end.
        /// </summary>
        Task DeleteByDeviceAsync(string macAddress, CancellationToken cancellationToken);

        /// <summary>
        /// Remove a policy, freeing its actuator.
        /// </summary>
        Task DeleteAsync(Policy policy, CancellationToken cancellationToken);
    }
}

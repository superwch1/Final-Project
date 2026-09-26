using Backend.Models;

namespace Backend.Repositories
{
    public interface IPolicyRepository
    {

        /// <summary>
        /// Finds a policy by ID with its sensor and actuator, or null if none exists
        /// </summary>
        Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken);


        /// <summary>
        /// Returns every policy that watches by a given sensor
        /// </summary>
        Task<List<Policy>> FindBySensorAsync(string sensorMacAddress, CancellationToken cancellationToken);


        /// <summary>
        /// Returns every policy whose sensor is in a room owned by the account
        /// </summary>
        Task<List<Policy>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken);


        /// <summary>
        /// Add a new policy, returning false if the actuator is already driven by another policy.
        /// </summary>
        Task<bool> TryAddAsync(Policy policy, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing policy.
        /// </summary>
        Task UpdateAsync(Policy policy, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes every policy that uses the device
        /// </summary>
        Task DeleteByDeviceAsync(string macAddress, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a policy
        /// </summary>
        Task DeleteAsync(Policy policy, CancellationToken cancellationToken);
    }
}

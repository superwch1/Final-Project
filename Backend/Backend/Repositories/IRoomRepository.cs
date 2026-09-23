using Backend.Models;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for rooms. Devices come with the room.
    /// </summary>
    public interface IRoomRepository
    {
        /// <summary>
        /// Find a room by id.
        /// </summary>
        Task<Room?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// List the rooms owned by an account.
        /// </summary>
        Task<List<Room>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken);

        /// <summary>
        /// Add a new room.
        /// </summary>
        Task AddAsync(Room room, CancellationToken cancellationToken);

        /// <summary>
        /// Save changes to a room.
        /// </summary>
        Task UpdateAsync(Room room, CancellationToken cancellationToken);

        /// <summary>
        /// Remove a room and the devices paired into it.
        /// </summary>
        Task DeleteAsync(Room room, CancellationToken cancellationToken);
    }
}

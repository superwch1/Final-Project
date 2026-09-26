using Backend.Models;

namespace Backend.Repositories
{
    public interface IRoomRepository
    {
        /// <summary>
        /// Finds a room by ID with its devices, or null if none exists
        /// </summary>
        Task<Room?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Returns every room owned by the account with its devices
        /// </summary>
        Task<List<Room>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken);

        /// <summary>
        /// Add a new room
        /// </summary>
        Task AddAsync(Room room, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing room
        /// </summary>
        Task UpdateAsync(Room room, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a room
        /// </summary>
        Task DeleteAsync(Room room, CancellationToken cancellationToken);
    }
}

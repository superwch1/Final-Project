using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for rooms. Devices come with the room.
    /// </summary>
    public sealed class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _dbContext;

        public RoomRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Room?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Rooms
                .Include(x => x.Devices)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<List<Room>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return _dbContext.Rooms
                .AsNoTracking()
                .Include(x => x.Devices)
                .Where(x => x.AccountId == accountId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Room room, CancellationToken cancellationToken)
        {
            _dbContext.Rooms.Add(room);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Room room, CancellationToken cancellationToken)
        {
            _dbContext.Rooms.Update(room);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Room room, CancellationToken cancellationToken)
        {
            _dbContext.Rooms.Remove(room);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

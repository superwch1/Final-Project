using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public sealed class PolicyRepository : IPolicyRepository
    {
        private readonly AppDbContext _dbContext;

        public PolicyRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Policies
                .Include(x => x.Sensor)
                .Include(x => x.Actuator)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<Policy>> FindBySensorAsync(string sensorMacAddress, CancellationToken cancellationToken)
        {
            return _dbContext.Policies
                .AsNoTracking()
                .Where(x => x.SensorMacAddress == sensorMacAddress)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<Policy>> FindByAccountAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return _dbContext.Policies
                .AsNoTracking()
                .Include(x => x.Sensor)
                .Include(x => x.Actuator)
                .Where(policy => _dbContext.Rooms.Any(room =>
                    (room.AccountId == accountId) && (room.Id == policy.Sensor!.RoomId)))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> TryAddAsync(Policy policy, CancellationToken cancellationToken)
        {
            bool isDriven = await _dbContext.Policies
                .AnyAsync(x => x.ActuatorMacAddress == policy.ActuatorMacAddress, cancellationToken);

            if (isDriven)
            {
                return false;
            }

            _dbContext.Policies.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Policy policy, CancellationToken cancellationToken)
        {
            _dbContext.Policies.Update(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteByDeviceAsync(string macAddress, CancellationToken cancellationToken)
        {
            await _dbContext.Policies
                .Where(x => (x.SensorMacAddress == macAddress) || (x.ActuatorMacAddress == macAddress))
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Policy policy, CancellationToken cancellationToken)
        {
            _dbContext.Policies.Remove(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for automation policies.
    /// </summary>
    public sealed class PolicyRepository : IPolicyRepository
    {
        private readonly AppDbContext _dbContext;

        public PolicyRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Policies
                .Include(x => x.Sensor)
                .Include(x => x.Actuator)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<List<Policy>> FindBySensorAsync(string sensorMacAddress, CancellationToken cancellationToken)
        {
            return _dbContext.Policies
                .AsNoTracking()
                .Where(x => x.SensorMacAddress == sensorMacAddress)
                .ToListAsync(cancellationToken);
        }

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

        public async Task UpdateAsync(Policy policy, CancellationToken cancellationToken)
        {
            _dbContext.Policies.Update(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteByDeviceAsync(string macAddress, CancellationToken cancellationToken)
        {
            await _dbContext.Policies
                .Where(x => (x.SensorMacAddress == macAddress) || (x.ActuatorMacAddress == macAddress))
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task DeleteAsync(Policy policy, CancellationToken cancellationToken)
        {
            _dbContext.Policies.Remove(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

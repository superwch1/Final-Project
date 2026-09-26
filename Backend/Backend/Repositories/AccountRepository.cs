using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _dbContext;

        public AccountRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task<Account?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

  
        /// <inheritdoc/>
        public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }


        /// <inheritdoc/>
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(x => x.Email == email, cancellationToken);
        }


        /// <inheritdoc/>
        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Add(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }


        /// <inheritdoc/>
        public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }


        /// <inheritdoc/>
        public async Task DeleteAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Remove(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for accounts.
    /// </summary>
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _dbContext;

        public AccountRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Find an account by id.
        /// </summary>
        public Task<Account?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <summary>
        /// Check whether an account with the email exists.
        /// </summary>
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(x => x.Email == email, cancellationToken);
        }

        /// <summary>
        /// Add a new account.
        /// </summary>
        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Add(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Save changes to an account.
        /// </summary>
        public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Remove an account.
        /// </summary>
        public async Task DeleteAsync(Account account, CancellationToken cancellationToken)
        {
            _dbContext.Accounts.Remove(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using Backend.Models;

namespace Backend.Repositories
{
    /// <summary>
    /// Database operations for accounts.
    /// </summary>
    public interface IAccountRepository
    {
        /// <summary>
        /// Find an account by id.
        /// </summary>
        Task<Account?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Find an account by email.
        /// </summary>
        Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken);

        /// <summary>
        /// Check whether an account with the email exists.
        /// </summary>
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

        /// <summary>
        /// Add a new account.
        /// </summary>
        Task AddAsync(Account account, CancellationToken cancellationToken);

        /// <summary>
        /// Save changes to an account.
        /// </summary>
        Task UpdateAsync(Account account, CancellationToken cancellationToken);

        /// <summary>
        /// Remove an account.
        /// </summary>
        Task DeleteAsync(Account account, CancellationToken cancellationToken);
    }
}

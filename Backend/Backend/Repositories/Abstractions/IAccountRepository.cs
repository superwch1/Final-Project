using Backend.Models;

namespace Backend.Repositories
{
    public interface IAccountRepository
    {

        /// <summary>
        /// Finds an account by ID, or null if none exists
        /// </summary>
        Task<Account?> FindByIdAsync(Guid id, CancellationToken cancellationToken);


        /// <summary>
        /// Finds an account by normalised email, or null if none exists
        /// </summary>
        Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken);


        /// <summary>
        /// Returns true if an account already uses the normalised email
        /// </summary>
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);


        /// <summary>
        /// Create a new account
        /// </summary>
        Task AddAsync(Account account, CancellationToken cancellationToken);


        /// <summary>
        /// Update an account
        /// </summary>
        Task UpdateAsync(Account account, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes an account
        /// </summary>
        Task DeleteAsync(Account account, CancellationToken cancellationToken);
    }
}

using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// Issues access tokens for accounts.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Create a signed access token for an account.
        /// </summary>
        string CreateToken(Account account);
    }
}

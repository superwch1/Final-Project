using Backend.Models;

namespace Backend.Services
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Creates a signed access token for the account
        /// </summary>
        string CreateToken(Account account);
    }
}

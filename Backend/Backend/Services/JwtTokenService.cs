using Backend.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Backend.Services
{
    /// <summary>
    /// Issues access tokens for accounts.
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _options;
        private readonly SigningCredentials _signingCredentials;
        private readonly JsonWebTokenHandler _tokenHandler = new();

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_options.Key));
            _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }

        /// <summary>
        /// Create a signed access token for an account.
        /// </summary>
        public string CreateToken(Account account)
        {
            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = (DateTimeOffset.UtcNow + _options.Lifetime).UtcDateTime,
                SigningCredentials = _signingCredentials,
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = account.Id.ToString(),
                    [JwtRegisteredClaimNames.Email] = account.Email,
                    [JwtRegisteredClaimNames.Name] = account.Name
                }
            };

            return _tokenHandler.CreateToken(descriptor);
        }
    }
}

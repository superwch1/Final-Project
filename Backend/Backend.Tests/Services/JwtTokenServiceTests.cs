using Backend.Models;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _jwtTokenService = new(Options.Create(TestOptions.Jwt));
        private readonly JsonWebTokenHandler _tokenHandler = new();
        private readonly Account _account = TestData.Account();

        [Fact]
        public void CreateToken_Account_HasAccountIdAsSubject()
        {
            JsonWebToken token = _tokenHandler.ReadJsonWebToken(_jwtTokenService.CreateToken(_account));

            Assert.Equal(_account.Id.ToString(), token.Subject);
        }

        [Fact]
        public void CreateToken_Account_HasEmailClaim()
        {
            JsonWebToken token = _tokenHandler.ReadJsonWebToken(_jwtTokenService.CreateToken(_account));

            Assert.Equal(_account.Email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
        }

        [Fact]
        public void CreateToken_Account_HasNameClaim()
        {
            JsonWebToken token = _tokenHandler.ReadJsonWebToken(_jwtTokenService.CreateToken(_account));

            Assert.Equal(_account.Name, token.GetClaim(JwtRegisteredClaimNames.Name).Value);
        }

        [Fact]
        public void CreateToken_Account_HasConfiguredIssuer()
        {
            JsonWebToken token = _tokenHandler.ReadJsonWebToken(_jwtTokenService.CreateToken(_account));

            Assert.Equal(TestOptions.Jwt.Issuer, token.Issuer);
        }

        [Fact]
        public void CreateToken_Account_ExpiresAfterLifetime()
        {
            DateTime expected = DateTime.UtcNow + TestOptions.Jwt.Lifetime;

            JsonWebToken token = _tokenHandler.ReadJsonWebToken(_jwtTokenService.CreateToken(_account));

            Assert.True((token.ValidTo - expected).Duration() < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateToken_Account_PassesValidationWithSigningKey()
        {
            string token = _jwtTokenService.CreateToken(_account);

            TokenValidationResult result = await _tokenHandler.ValidateTokenAsync(token, TestOptions.JwtBearer().Get("").TokenValidationParameters);

            Assert.True(result.IsValid);
        }
    }
}

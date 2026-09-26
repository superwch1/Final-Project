using Backend.Enumerations;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Tests.Helpers
{
    public static class TestOptions
    {
        public const string MasterKey = "test-master-key";

        /// <summary>
        /// JWT settings with a key
        /// </summary>
        public static JwtOptions Jwt { get; } = new()
        {
            Key = "test-signing-key-that-is-long-enough-for-hmac",
            Issuer = "test-issuer",
            Audience = "test-audience",
            Lifetime = TimeSpan.FromMinutes(30)
        };

        /// <summary>
        /// Device settings with a 30 second clock skew
        /// </summary>
        public static DeviceOptions Device { get; } = new()
        {
            MasterKey = MasterKey,
            MaxClockSkew = TimeSpan.FromSeconds(30),
            ClockSyncInterval = TimeSpan.FromMilliseconds(50)
        };

        /// <summary>
        /// Bearer options that accept tokens made by a JwtTokenService
        /// </summary>
        public static IOptionsMonitor<JwtBearerOptions> JwtBearer()
        {
            JwtBearerOptions options = new()
            {
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Jwt.Issuer,
                    ValidAudience = Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt.Key))
                }
            };

            Mock<IOptionsMonitor<JwtBearerOptions>> monitor = new();
            monitor.Setup(x => x.Get(It.IsAny<string>())).Returns(options);

            return monitor.Object;
        }

        /// <summary>
        /// Build a device message
        /// </summary>
        public static string SignedMessage(string macAddress, DeviceType deviceType, long timestamp, string data, string masterKey = MasterKey)
        {
            DeviceKeyService deviceKeyService = new();
            byte[] key = Encoding.UTF8.GetBytes(deviceKeyService.Derive(masterKey, macAddress));
            string signed = $"{deviceKeyService.NormalizeMacAddress(macAddress)}|{deviceType}|{timestamp}|{data}";
            string signature = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signed)));

            return $"{{\"timestamp\":{timestamp},\"signature\":\"{signature}\",\"data\":{data}}}";
        }

        /// <summary>
        /// The current time in Unix milliseconds
        /// </summary>
        public static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

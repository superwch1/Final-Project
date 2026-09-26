using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public sealed class DeviceKeyService : IDeviceKeyService
    {
        /// <inheritdoc/>
        public string Derive(string masterKey, string macAddress)
        {
            byte[] master = Encoding.UTF8.GetBytes(masterKey);
            byte[] label = Encoding.UTF8.GetBytes(NormalizeMacAddress(macAddress));

            return Convert.ToHexString(HMACSHA256.HashData(master, label));
        }

        /// <inheritdoc/>
        public string NormalizeMacAddress(string macAddress)
        {
            return macAddress
                .Replace(":", string.Empty)
                .Replace("-", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }
    }
}

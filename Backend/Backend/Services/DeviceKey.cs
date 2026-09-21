using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    /// <summary>
    /// Derives a per-device key from the master key and a MAC address.
    /// </summary>
    public static class DeviceKey
    {
        /// <summary>
        /// Derive the key for device
        /// </summary>
        public static string Derive(string masterKey, string macAddress)
        {
            byte[] master = Encoding.UTF8.GetBytes(masterKey);
            byte[] label = Encoding.UTF8.GetBytes(NormalizeMacAddress(macAddress));

            return Convert.ToHexString(HMACSHA256.HashData(master, label));
        }

        /// <summary>
        /// Strip separators and upper-case, so the board and the server derive the same key.
        /// </summary>
        public static string NormalizeMacAddress(string macAddress)
        {
            return macAddress
                .Replace(":", string.Empty)
                .Replace("-", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }
    }
}

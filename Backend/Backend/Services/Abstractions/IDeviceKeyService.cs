namespace Backend.Services
{
    public interface IDeviceKeyService
    {
        /// <summary>
        /// Derives a device's signing key from the master key and its MAC address
        /// </summary>
        string Derive(string masterKey, string macAddress);

        /// <summary>
        /// Removes separators and whitespace from a MAC address
        /// </summary>
        string NormalizeMacAddress(string macAddress);
    }
}

namespace Backend.Models
{
    /// <summary>
    /// Settings for verifying device messages.
    /// </summary>
    public class DeviceOptions
    {
        public const string SectionName = "Device";

        /// <summary>
        /// Every device key is derived from this.
        /// </summary>
        public required string MasterKey { get; set; }

        /// <summary>
        /// How far a device clock can differ from the server.
        /// </summary>
        public required TimeSpan MaxClockSkew { get; set; }

        /// <summary>
        /// How often the server resends its clock.
        /// </summary>
        public required TimeSpan ClockSyncInterval { get; set; }
    }
}

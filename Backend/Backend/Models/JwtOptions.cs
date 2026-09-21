namespace Backend.Authentication
{
    /// <summary>
    /// Signing and validation settings for the access tokens.
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>
        /// The HMAC-SHA256 signing key.
        /// </summary>
        public required string Key { get; set; }

        /// <summary>
        /// The issuer of the token.
        /// </summary>
        public required string Issuer { get; set; } 

        /// <summary>
        /// The audience of the token.
        /// </summary>
        public required string Audience { get; set; } 

        /// <summary>
        /// How long an issued token stays valid.
        /// </summary>
        public required TimeSpan Lifetime { get; set; } 
    }
}

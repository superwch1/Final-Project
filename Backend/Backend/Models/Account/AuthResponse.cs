namespace Backend.Models
{
    /// <summary>
    /// A signed access token.
    /// </summary>
    public record AuthResponse
    {
        public required string AccessToken { get; init; }
    }
}

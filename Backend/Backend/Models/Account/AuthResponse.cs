namespace Backend.Models
{
    public record AuthResponse
    {
        public required string AccessToken { get; init; }
    }
}

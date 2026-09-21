using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// Credentials for signing in.
    /// </summary>
    public record LoginRequest
    {
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; init; }

        [MaxLength(128)]
        public required string Password { get; init; }
    }
}

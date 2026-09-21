using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// Details for a new account.
    /// </summary>
    public record CreateAccountRequest
    {
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; init; }

        [MinLength(8)]
        [MaxLength(128)]
        public required string Password { get; init; }

        [MinLength(1)]
        [MaxLength(128)]
        public required string Name { get; init; }
    }
}

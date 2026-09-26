using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public record CreateAccountRequest
    {
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; init; }

        [MinLength(PasswordRules.MinimumLength, ErrorMessage = PasswordRules.Message)]
        [MaxLength(PasswordRules.MaximumLength)]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        public required string Password { get; init; }

        [MinLength(1)]
        [MaxLength(128)]
        public required string Name { get; init; }
    }
}

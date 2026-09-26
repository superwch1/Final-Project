using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public record UpdateAccountRequest
    {
        [MinLength(1)]
        [MaxLength(128)]
        public string? Name { get; init; }

        [MinLength(PasswordRules.MinimumLength, ErrorMessage = PasswordRules.Message)]
        [MaxLength(PasswordRules.MaximumLength)]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        public string? NewPassword { get; init; }
    }
}

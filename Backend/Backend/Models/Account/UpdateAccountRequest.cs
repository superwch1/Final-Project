using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// Changes to apply to the signed-in account. Null means leave unchanged.
    /// </summary>
    public record UpdateAccountRequest
    {
        [MinLength(1)]
        [MaxLength(128)]
        public string? Name { get; init; }

        [MinLength(8)]
        [MaxLength(128)]
        public string? NewPassword { get; init; }
    }
}

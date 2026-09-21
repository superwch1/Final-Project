using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public record Account
    {
        public required Guid Id { get; set; }

        [MaxLength(256)]
        public required string Email { get; set; }

        [MaxLength(512)]
        public required string PasswordHash { get; set; }

        [MaxLength(128)]
        public required string Name { get; set; }

        public required int FailedAttemptCount { get; set; }

        public DateTimeOffset? LastFailedAttemptUtc { get; set; }

        public DateTimeOffset? LockoutEndUtc { get; set; }
    }
}

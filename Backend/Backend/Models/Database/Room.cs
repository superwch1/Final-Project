using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    [Index(nameof(AccountId))]
    public record Room
    {
        public required Guid Id { get; set; }

        public required Guid AccountId { get; set; }

        [MaxLength(128)]
        public required string Name { get; set; }

        public Account? Account { get; set; }

        public List<Device> Devices { get; set; } = [];
    }
}

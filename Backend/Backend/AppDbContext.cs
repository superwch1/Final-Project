using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<Device> Devices { get; set; }
    }
}

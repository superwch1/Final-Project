using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Helpers
{
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new AppDbContext(_options);
            Context.Database.EnsureCreated();
        }

        public AppDbContext Context { get; }

        public AppDbContext NewContext()
        {
            return new AppDbContext(_options);
        }

        public async Task SeedAsync(params object[] entities)
        {
            using AppDbContext context = NewContext();
            context.AddRange(entities);
            await context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}

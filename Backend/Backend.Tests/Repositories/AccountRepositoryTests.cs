using Backend.Models;
using Backend.Repositories;
using Backend.Tests.Helpers;

namespace Backend.Tests.Repositories
{
    public sealed class AccountRepositoryTests : IDisposable
    {
        private readonly TestDatabase _database = new();
        private readonly AccountRepository _accountRepository;

        public AccountRepositoryTests()
        {
            _accountRepository = new AccountRepository(_database.Context);
        }

        [Fact]
        public async Task AddAsync_NewAccount_SavesAccount()
        {
            Account account = TestData.Account();

            await _accountRepository.AddAsync(account, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.NotNull(await context.Accounts.FindAsync(account.Id));
        }

        [Fact]
        public async Task FindByIdAsync_ExistingAccount_ReturnsAccount()
        {
            Account account = TestData.Account();
            await _database.SeedAsync(account);

            Account? found = await _accountRepository.FindByIdAsync(account.Id, CancellationToken.None);

            Assert.Equal(account.Email, found?.Email);
        }

        [Fact]
        public async Task FindByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _accountRepository.FindByIdAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task FindByEmailAsync_ExistingEmail_ReturnsAccount()
        {
            Account account = TestData.Account();
            await _database.SeedAsync(account);

            Account? found = await _accountRepository.FindByEmailAsync(account.Email, CancellationToken.None);

            Assert.Equal(account.Id, found?.Id);
        }

        [Fact]
        public async Task FindByEmailAsync_UnknownEmail_ReturnsNull()
        {
            Assert.Null(await _accountRepository.FindByEmailAsync("NOBODY@EXAMPLE.COM", CancellationToken.None));
        }

        [Fact]
        public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
        {
            Account account = TestData.Account();
            await _database.SeedAsync(account);

            Assert.True(await _accountRepository.EmailExistsAsync(account.Email, CancellationToken.None));
        }

        [Fact]
        public async Task EmailExistsAsync_UnknownEmail_ReturnsFalse()
        {
            Assert.False(await _accountRepository.EmailExistsAsync("NOBODY@EXAMPLE.COM", CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_ChangedName_SavesName()
        {
            Account account = TestData.Account();
            await _database.SeedAsync(account);
            account.Name = "Renamed";

            await _accountRepository.UpdateAsync(account, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Equal("Renamed", (await context.Accounts.FindAsync(account.Id))?.Name);
        }

        [Fact]
        public async Task DeleteAsync_ExistingAccount_RemovesAccount()
        {
            Account account = TestData.Account();
            await _database.SeedAsync(account);

            await _accountRepository.DeleteAsync(account, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Null(await context.Accounts.FindAsync(account.Id));
        }

        public void Dispose()
        {
            _database.Dispose();
        }
    }
}

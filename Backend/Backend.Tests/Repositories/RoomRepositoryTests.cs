using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Tests.Helpers;

namespace Backend.Tests.Repositories
{
    public sealed class RoomRepositoryTests : IAsyncLifetime
    {
        private readonly TestDatabase _database = new();
        private readonly RoomRepository _roomRepository;
        private readonly Account _account = TestData.Account();

        public RoomRepositoryTests()
        {
            _roomRepository = new RoomRepository(_database.Context);
        }

        /// <summary>
        /// Seeds the account
        /// </summary>
        public Task InitializeAsync()
        {
            return _database.SeedAsync(_account);
        }

        [Fact]
        public async Task AddAsync_NewRoom_SavesRoom()
        {
            Room room = TestData.Room(_account.Id);

            await _roomRepository.AddAsync(room, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.NotNull(await context.Rooms.FindAsync(room.Id));
        }

        [Fact]
        public async Task FindByIdAsync_RoomWithDevice_IncludesDevice()
        {
            Room room = TestData.Room(_account.Id);
            await _database.SeedAsync(room, TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, room.Id));

            Room? found = await _roomRepository.FindByIdAsync(room.Id, CancellationToken.None);

            Assert.Single(found!.Devices);
        }

        [Fact]
        public async Task FindByIdAsync_UnknownRoom_ReturnsNull()
        {
            Assert.Null(await _roomRepository.FindByIdAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task FindByAccountAsync_RoomsOfTwoAccounts_ReturnsOnlyAccountRooms()
        {
            Account otherAccount = TestData.Account("OTHER@EXAMPLE.COM");
            Room room = TestData.Room(_account.Id);
            await _database.SeedAsync(otherAccount, room, TestData.Room(otherAccount.Id));

            List<Room> rooms = await _roomRepository.FindByAccountAsync(_account.Id, CancellationToken.None);

            Assert.Equal(room.Id, Assert.Single(rooms).Id);
        }

        [Fact]
        public async Task FindByAccountAsync_SeveralRooms_OrdersByName()
        {
            await _database.SeedAsync(TestData.Room(_account.Id, "Office"), TestData.Room(_account.Id, "Bedroom"));

            List<Room> rooms = await _roomRepository.FindByAccountAsync(_account.Id, CancellationToken.None);

            Assert.Equal(["Bedroom", "Office"], rooms.Select(room => room.Name));
        }

        [Fact]
        public async Task UpdateAsync_ChangedName_SavesName()
        {
            Room room = TestData.Room(_account.Id);
            await _database.SeedAsync(room);
            room.Name = "Kitchen";

            await _roomRepository.UpdateAsync(room, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Equal("Kitchen", (await context.Rooms.FindAsync(room.Id))?.Name);
        }

        [Fact]
        public async Task DeleteAsync_EmptyRoom_RemovesRoom()
        {
            Room room = TestData.Room(_account.Id);
            await _database.SeedAsync(room);

            await _roomRepository.DeleteAsync(room, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Null(await context.Rooms.FindAsync(room.Id));
        }

        [Fact]
        public async Task DeleteAsync_RoomWithDevice_UnpairsDevice()
        {
            Room room = TestData.Room(_account.Id);
            await _database.SeedAsync(room, TestData.Device(TestData.SensorMacAddress, DeviceType.LightSensor, room.Id));
            Room? loaded = await _roomRepository.FindByIdAsync(room.Id, CancellationToken.None);

            await _roomRepository.DeleteAsync(loaded!, CancellationToken.None);

            using AppDbContext context = _database.NewContext();
            Assert.Null((await context.Devices.FindAsync(TestData.SensorMacAddress))?.RoomId);
        }

        public Task DisposeAsync()
        {
            _database.Dispose();
            return Task.CompletedTask;
        }
    }
}

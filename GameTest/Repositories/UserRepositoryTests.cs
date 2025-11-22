using System;
using System.Threading.Tasks;
using Backend.Persistence;
using Backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameTest.Repositories;

public class UserRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _dbContext = null!;
    private UserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new UserRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task RegisterAsync_PersistsUserWithHashedPassword()
    {
        var user = await _repository.RegisterAsync("alice", "secret1");

        Assert.NotEqual(0, user.Id);
        Assert.NotNull(user.PassHash);
        Assert.NotEqual("secret1", user.PassHash);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsForDuplicateUsername()
    {
        await _repository.RegisterAsync("bob", "hunter2");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.RegisterAsync("bob", "another"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsUserWhenPasswordMatches()
    {
        await _repository.RegisterAsync("claire", "strongpass");

        var user = await _repository.LoginAsync("claire", "strongpass");

        Assert.NotNull(user);
        Assert.Equal("claire", user!.Username);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForInvalidPassword()
    {
        await _repository.RegisterAsync("dave", "solidpw");

        var user = await _repository.LoginAsync("dave", "wrong");

        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsForEmptyUsername()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.RegisterAsync("", "validpass"));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsForShortPassword()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.RegisterAsync("user", "123"));
    }
}

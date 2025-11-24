using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backend.Persistence;
using Backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameTest.Repositories;

public class GameRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _dbContext = null!;
    private GameRepository _repository = null!;
    private UserRepository _userRepo = null!;

    public async Task InitializeAsync()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appData, "MyGame", "db.sqlite");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new GameRepository(_dbContext);
        _userRepo = new UserRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SaveGameAsync_PersistsGameWithJsonMoves()
    {
        var user = await _userRepo.RegisterAsync("gamer", "pass123");
        var moves = new List<string> { "move1", "move2" };

        var game = await _repository.SaveGameAsync(user.Id, "AI", "Win", moves);

        Assert.NotEqual(0, game.Id);
        Assert.Equal(user.Id, game.UserId);
        Assert.Equal("AI", game.Opponent);
        Assert.Equal("Win", game.Result);
        Assert.Contains("move1", game.MovesJson);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsGamesOrderedByCreatedAtDescending()
    {
        var user = await _userRepo.RegisterAsync("historyuser", "pass123");
        var game1 = await _repository.SaveGameAsync(user.Id, "AI", "Win", new List<string> { "m1" });
        var game2 = await _repository.SaveGameAsync(user.Id, "Human", "Loss", new List<string> { "m2" });

        var history = await _repository.GetHistoryAsync(user.Id);

        Assert.Equal(2, history.Count);
        Assert.Equal(game2.Id, history[0].Id); // newest first
        Assert.Equal(game1.Id, history[1].Id);
    }

    [Fact]
    public async Task GetHistoryAsync_LimitsResults()
    {
        var user = await _userRepo.RegisterAsync("limituser", "pass123");
        for (int i = 0; i < 5; i++)
        {
            await _repository.SaveGameAsync(user.Id, "AI", "Draw", new List<string> { $"move{i}" });
        }

        var history = await _repository.GetHistoryAsync(user.Id, 3);

        Assert.Equal(3, history.Count);
    }
}

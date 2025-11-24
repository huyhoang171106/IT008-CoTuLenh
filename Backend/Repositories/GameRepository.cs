using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Backend.Entities;
using Backend.Persistence;

namespace Backend.Repositories;

public class GameRepository
{
    private readonly AppDbContext _dbContext;

    public GameRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Game> SaveGameAsync(int userId, string opponent, string result, object moves)
    {
        if (moves == null)
            throw new ArgumentNullException(nameof(moves));

        var movesJson = JsonSerializer.Serialize(moves);
        var game = new Game
        {
            UserId = userId,
            Opponent = opponent ?? string.Empty,
            Result = result ?? string.Empty,
            MovesJson = movesJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();
        return game;
    }

    public Task<List<Game>> GetHistoryAsync(int userId, int limit = 100)
    {
        return _dbContext.Games
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}


using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Backend.Entities;
using Backend.Persistence;

namespace Backend.Repositories;

public class UserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));
        if (password is null || password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters", nameof(password));

        var exists = await _dbContext.Users.AnyAsync(u => u.Username == username);
        if (exists)
            throw new InvalidOperationException("Username already taken");

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Username = username,
            PassHash = hash,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return null;

        var valid = BCrypt.Net.BCrypt.Verify(password, user.PassHash);
        return valid ? user : null;
    }
}


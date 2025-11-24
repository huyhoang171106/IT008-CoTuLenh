using System;

namespace Backend.Entities;

public class Game
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string MovesJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

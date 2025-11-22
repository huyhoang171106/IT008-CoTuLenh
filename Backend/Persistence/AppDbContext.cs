using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Backend.Entities;

namespace Backend.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "MyGame");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "db.sqlite");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired();
            entity.Property(u => u.PassHash).HasColumnName("passhash").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("createdAt").IsRequired();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.MovesJson).HasColumnName("movesJson").HasColumnType("TEXT").IsRequired();
            entity.Property(g => g.Opponent).HasColumnName("opponent").IsRequired();
            entity.Property(g => g.Result).HasColumnName("result").IsRequired();
            entity.Property(g => g.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(g => g.User)
                .WithMany(u => u.Games)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

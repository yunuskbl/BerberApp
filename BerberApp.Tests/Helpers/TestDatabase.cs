using BerberApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Tests.Helpers;

/// <summary>Her test için izole InMemory veritabanı oluşturur.</summary>
public static class TestDatabase
{
    public static AppDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}

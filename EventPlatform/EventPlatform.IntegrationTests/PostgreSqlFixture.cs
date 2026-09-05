using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventPlatform.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .Build();

    public static readonly TimeSpan DatePrecision = TimeSpan.FromMilliseconds(1);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE""");
    }

    public static Event NewEvent(
        string title = "Test event",
        int seats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddHours(1);
        var end = endAt ?? start.AddHours(2);
        return new Event(Guid.NewGuid(), title, start, end, seats);
    }
}

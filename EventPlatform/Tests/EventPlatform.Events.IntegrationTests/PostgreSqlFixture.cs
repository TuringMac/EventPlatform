using EventPlatform.Events.Domain.Model;
using EventPlatform.Events.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventPlatform.Events.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("eventplatform_events_tests")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(TestContext.Current.CancellationToken);
        await using var context = CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options);

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events RESTART IDENTITY CASCADE", TestContext.Current.CancellationToken);
    }

    public static Event NewEvent(string title = "Test event", int seats = 10, DateTime? startAt = null, DateTime? endAt = null)
    {
        const int startOffsetHours = 1;
        const int durationHours = 2;
        var start = startAt ?? DateTime.UtcNow.AddHours(startOffsetHours);
        return new Event(Guid.NewGuid(), title, start, endAt ?? start.AddHours(durationHours), seats);
    }
}

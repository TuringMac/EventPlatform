using EventPlatform.Users.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventPlatform.Users.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("eventplatform_users_tests")
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
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE", TestContext.Current.CancellationToken);
    }
}

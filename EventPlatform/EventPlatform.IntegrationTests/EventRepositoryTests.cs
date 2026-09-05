using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Model;
using EventPlatform.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel;
using Testcontainers.PostgreSql;

namespace EventPlatform.IntegrationTests;

public class EventRepositoryTests : IAsyncLifetime
{
    #region Infrastructure

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();
    private static readonly TimeSpan DatePrecision = TimeSpan.FromMilliseconds(1);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        // Сбрасываем пул — иначе PostgreSQL не даст удалить базу
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE""");
    }

    private static Event NewEvent(
        string title = "Test event",
        int seats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddHours(1);
        var end = endAt ?? start.AddHours(2);
        return new Event(Guid.NewGuid(), title, start, end, seats);
    }

    #endregion Infrastructure

    [Fact]
    public async Task AddAsync_PersistsEvent()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt = NewEvent("Conference");
        var repository = new EventRepository(context);

        // Act
        await repository.AddAsync(evt);

        // Assert — читаем из реальной БД через отдельный контекст
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.SingleAsync(e => e.Id == evt.Id);

        saved.Title.Should().Be("Conference");
        saved.TotalSeats.Should().Be(evt.TotalSeats);
        saved.AvailableSeats.Should().Be(evt.TotalSeats);
        saved.StartAt.Should().BeCloseTo(evt.StartAt, DatePrecision);
        saved.EndAt.Should().BeCloseTo(evt.EndAt, DatePrecision);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEventWithBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = NewEvent();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.Add(evt);
            arrangeContext.Bookings.Add(new Booking(evt.Id));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(evt.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(evt.Id);
        loaded.Bookings.Should().ContainSingle(b => b.EventId == evt.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsFieldChanges()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = NewEvent();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id);
        tracked!.Title = "Updated title";
        tracked.Description = "Updated description";
        var newEnd = tracked.EndAt.AddHours(3);
        tracked.EndAt = newEnd;

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        saved.Title.Should().Be("Updated title");
        saved.Description.Should().Be("Updated description");
        saved.EndAt.Should().BeCloseTo(newEnd, DatePrecision);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAvailableSeats()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = NewEvent(seats: 5);
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id);
        tracked!.TryReserveSeats().Should().BeTrue();

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        saved.AvailableSeats.Should().Be(4);
        saved.TotalSeats.Should().Be(5);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = NewEvent();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var deleted = await repository.DeleteAsync(evt.Id);

        // Assert
        deleted.Should().Be(1);
        await using var verify = CreateContext();
        (await verify.Events.AnyAsync(e => e.Id == evt.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsZero()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var deleted = await repository.DeleteAsync(Guid.NewGuid());

        // Assert
        deleted.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPage()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.AddRange(
                NewEvent("Event A"),
                NewEvent("Event B"),
                NewEvent("Event C"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, currentPage, pageItems, totalAmount) =
            await repository.GetPagedAsync(null, null, null, page: 1, pageSize: 2);

        // Assert
        currentPage.Should().Be(1);
        pageItems.Should().Be(2);
        totalAmount.Should().Be(3);
        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.AddRange(
                NewEvent("Alpha Concert"),
                NewEvent("Beta Meetup"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, _, pageItems, totalAmount) =
            await repository.GetPagedAsync("concert", null, null, page: 1, pageSize: 10);

        // Assert
        totalAmount.Should().Be(1);
        pageItems.Should().Be(1);
        events.Should().ContainSingle(e => e.Title == "Alpha Concert");
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        // Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        var early = NewEvent("Early", startAt: now.AddHours(-8), endAt: now.AddHours(-2));
        var mid = NewEvent("Mid", startAt: now.AddHours(-1), endAt: now.AddHours(1));
        var late = NewEvent("Late", startAt: now.AddHours(2), endAt: now.AddHours(8));

        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Events.AddRange(early, mid, late);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, _, _, totalAmount) = await repository.GetPagedAsync(
            title: null,
            from: now.AddMinutes(-15),
            to: now.AddMinutes(15),
            page: 1,
            pageSize: 10);

        // Assert
        totalAmount.Should().Be(1);
        events.Should().ContainSingle(e => e.Id == mid.Id);
    }
}

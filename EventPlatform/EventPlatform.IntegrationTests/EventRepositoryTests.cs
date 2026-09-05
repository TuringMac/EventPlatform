using EventPlatform.Api.Model;
using EventPlatform.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.IntegrationTests;

public class EventRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static readonly TimeSpan DatePrecision = TimeSpan.FromMilliseconds(1);

    [Fact]
    public async Task AddAsync_PersistsEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var evt = PostgreSqlFixture.NewEvent("Conference");
        var repository = new EventRepository(context);

        // Act
        await repository.AddAsync(evt);

        // Assert — читаем из реальной БД через отдельный контекст
        await using var verifyContext = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            arrangeContext.Bookings.Add(new Booking(evt.Id));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id);
        tracked!.Title = "Updated title";
        tracked.Description = "Updated description";
        var newEnd = tracked.EndAt.AddHours(3);
        tracked.EndAt = newEnd;

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        saved.Title.Should().Be("Updated title");
        saved.Description.Should().Be("Updated description");
        saved.EndAt.Should().BeCloseTo(newEnd, DatePrecision);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAvailableSeats()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent(seats: 5);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id);
        tracked!.TryReserveSeats().Should().BeTrue();

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        saved.AvailableSeats.Should().Be(4);
        saved.TotalSeats.Should().Be(5);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var deleted = await repository.DeleteAsync(evt.Id);

        // Assert
        deleted.Should().Be(1);
        await using var verify = fixture.CreateContext();
        (await verify.Events.AnyAsync(e => e.Id == evt.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsZero()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(
                PostgreSqlFixture.NewEvent("Event A"),
                PostgreSqlFixture.NewEvent("Event B"),
                PostgreSqlFixture.NewEvent("Event C"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(
                PostgreSqlFixture.NewEvent("Alpha Concert"),
                PostgreSqlFixture.NewEvent("Beta Meetup"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
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
        await fixture.ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        var early = PostgreSqlFixture.NewEvent("Early", startAt: now.AddHours(-8), endAt: now.AddHours(-2));
        var mid = PostgreSqlFixture.NewEvent("Mid", startAt: now.AddHours(-1), endAt: now.AddHours(1));
        var late = PostgreSqlFixture.NewEvent("Late", startAt: now.AddHours(2), endAt: now.AddHours(8));

        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(early, mid, late);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
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

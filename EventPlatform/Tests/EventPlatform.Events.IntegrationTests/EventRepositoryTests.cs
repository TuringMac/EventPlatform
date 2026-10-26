using EventPlatform.Events.Domain.Model;
using EventPlatform.Events.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Events.IntegrationTests;

[Collection("PostgreSql")]
public class EventRepositoryTests(PostgreSqlFixture fixture)
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
        await repository.AddAsync(evt, TestContext.Current.CancellationToken);

        // Assert — читаем из реальной БД через отдельный контекст
        await using var verifyContext = fixture.CreateContext();
        var saved = await verifyContext.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);

        saved.Title.Should().Be("Conference");
        saved.TotalSeats.Should().Be(evt.TotalSeats);
        saved.AvailableSeats.Should().Be(evt.TotalSeats);
        saved.StartAt.Should().BeCloseTo(evt.StartAt, DatePrecision);
        saved.EndAt.Should().BeCloseTo(evt.EndAt, DatePrecision);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsFieldChanges()
    {
        // Arrange
        var endOffsetHours = 3;
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);
        tracked!.Title = "Updated title";
        tracked.Description = "Updated description";
        var newEnd = tracked.EndAt.AddHours(endOffsetHours);
        tracked.EndAt = newEnd;

        // Act
        await repository.UpdateAsync(evt.Id, tracked, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        saved.Title.Should().Be("Updated title");
        saved.Description.Should().Be("Updated description");
        saved.EndAt.Should().BeCloseTo(newEnd, DatePrecision);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAvailableSeats()
    {
        // Arrange
        var totalSeats = 5;
        var reservedSeats = 1;
        await fixture.ResetDatabaseAsync();
        var evt = PostgreSqlFixture.NewEvent(seats: totalSeats);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.Add(evt);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);
        var tracked = await repository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);
        tracked!.TryReserveSeats().Should().BeTrue();

        // Act
        await repository.UpdateAsync(evt.Id, tracked, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        saved.AvailableSeats.Should().Be(totalSeats - reservedSeats);
        saved.TotalSeats.Should().Be(totalSeats);
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
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        await repository.DeleteAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        (await verify.Events.AnyAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsKeyNotFoundException()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var act = () => repository.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPage()
    {
        // Arrange
        var page = 1;
        var pageSize = 2;
        var expectedTotal = 3;
        await fixture.ResetDatabaseAsync();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(
                PostgreSqlFixture.NewEvent("Event A"),
                PostgreSqlFixture.NewEvent("Event B"),
                PostgreSqlFixture.NewEvent("Event C"));
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, currentPage, pageItems, totalAmount) =
            await repository.GetPagedAsync(null, null, null, page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        currentPage.Should().Be(page);
        pageItems.Should().Be(pageSize);
        totalAmount.Should().Be(expectedTotal);
        events.Should().HaveCount(pageSize);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var expectedCount = 1;
        await fixture.ResetDatabaseAsync();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(
                PostgreSqlFixture.NewEvent("Alpha Concert"),
                PostgreSqlFixture.NewEvent("Beta Meetup"));
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, _, pageItems, totalAmount) =
            await repository.GetPagedAsync("concert", null, null, page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        totalAmount.Should().Be(expectedCount);
        pageItems.Should().Be(expectedCount);
        events.Should().ContainSingle(e => e.Title == "Alpha Concert");
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        // Arrange
        var earlyStartOffsetHours = -8;
        var earlyEndOffsetHours = -2;
        var midStartOffsetHours = -1;
        var midEndOffsetHours = 1;
        var lateStartOffsetHours = 2;
        var lateEndOffsetHours = 8;
        var fromOffsetMinutes = -15;
        var toOffsetMinutes = 15;
        var page = 1;
        var pageSize = 10;
        var expectedCount = 1;
        await fixture.ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        var early = PostgreSqlFixture.NewEvent("Early", startAt: now.AddHours(earlyStartOffsetHours), endAt: now.AddHours(earlyEndOffsetHours));
        var mid = PostgreSqlFixture.NewEvent("Mid", startAt: now.AddHours(midStartOffsetHours), endAt: now.AddHours(midEndOffsetHours));
        var late = PostgreSqlFixture.NewEvent("Late", startAt: now.AddHours(lateStartOffsetHours), endAt: now.AddHours(lateEndOffsetHours));

        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Events.AddRange(early, mid, late);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new EventRepository(context);

        // Act
        var (events, _, _, totalAmount) = await repository.GetPagedAsync(
            title: null,
            from: now.AddMinutes(fromOffsetMinutes),
            to: now.AddMinutes(toOffsetMinutes),
            page: page,
            pageSize: pageSize,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        totalAmount.Should().Be(expectedCount);
        events.Should().ContainSingle(e => e.Id == mid.Id);
    }
}

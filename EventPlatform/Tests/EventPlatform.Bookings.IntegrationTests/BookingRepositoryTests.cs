using EventPlatform.Bookings.Domain.Model;
using EventPlatform.Bookings.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Bookings.IntegrationTests;

[Collection("PostgreSql")]
public class BookingRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        await repository.AddAsync(booking, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        saved.EventId.Should().Be(booking.EventId);
        saved.Status.Should().Be(BookingStatusEnum.Pending);
        saved.ProcessedAt.Should().BeNull();
        saved.CreatedAt.Should().BeCloseTo(booking.CreatedAt, PostgreSqlFixture.DatePrecision);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await using (var arrange = fixture.CreateContext())
        {
            arrange.Bookings.Add(booking);
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(booking.Id);
        loaded.EventId.Should().Be(booking.EventId);
        loaded.Status.Should().Be(BookingStatusEnum.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsConfirmedStatus()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await using (var arrange = fixture.CreateContext())
        {
            arrange.Bookings.Add(booking);
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        var tracked = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);
        tracked!.Confirm(DateTime.UtcNow);

        // Act
        await repository.UpdateAsync(tracked, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(BookingStatusEnum.Confirmed);
        saved.ProcessedAt.Should().NotBeNull();
        saved.ProcessedAt.Should().BeCloseTo(tracked.ProcessedAt!.Value, PostgreSqlFixture.DatePrecision);
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPendingOrderedByCreatedAt()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var batchSize = 50;
        var creationDelayMilliseconds = 20;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var first = new Booking(eventId, userId);
        await Task.Delay(creationDelayMilliseconds, TestContext.Current.CancellationToken);
        var second = new Booking(eventId, userId);
        var confirmed = new Booking(eventId, userId);
        confirmed.Confirm(DateTime.UtcNow);
        await using (var arrange = fixture.CreateContext())
        {
            arrange.Bookings.AddRange(first, second, confirmed);
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batchSize, TestContext.Current.CancellationToken);

        // Assert
        pending.Should().Equal(first.Id, second.Id);
        pending.Should().NotContain(confirmed.Id);
    }

    [Fact]
    public async Task GetPendingIdsAsync_RespectsBatchSize()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var batchSize = 2;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using (var arrange = fixture.CreateContext())
        {
            arrange.Bookings.AddRange(new Booking(eventId, userId), new Booking(eventId, userId), new Booking(eventId, userId));
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batchSize, TestContext.Current.CancellationToken);

        // Assert
        pending.Should().HaveCount(batchSize);
    }

    [Fact]
    public async Task GetByUserIdAsync_AndGetByIdAsync_RestrictToOwner()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        await repository.AddAsync(booking, TestContext.Current.CancellationToken);

        // Act
        var own = await repository.GetByUserIdAsync(booking.UserId, TestContext.Current.CancellationToken);
        var another = await repository.GetByIdAsync(booking.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var ownerBooking = await repository.GetByIdAsync(booking.Id, booking.UserId, TestContext.Current.CancellationToken);

        // Assert
        own.Should().ContainSingle().Which.Id.Should().Be(booking.Id);
        another.Should().BeNull();
        ownerBooking!.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task CountUserBookings_ExcludesCancelledBookings()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var activeCount = 1;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var active = new Booking(eventId, userId);
        var cancelled = new Booking(eventId, userId);
        cancelled.Confirm(DateTime.UtcNow);
        cancelled.Cancel(DateTime.UtcNow);
        await using var context = fixture.CreateContext();
        context.Bookings.AddRange(active, cancelled);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new BookingRepository(context);

        // Act
        var count = await repository.CountUserBookings(userId, TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(activeCount);
    }
}

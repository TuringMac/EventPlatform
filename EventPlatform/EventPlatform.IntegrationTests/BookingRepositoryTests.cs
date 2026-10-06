using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.IntegrationTests;

[Collection("PostgreSql")]
public class BookingRepositoryTests(PostgreSqlFixture fixture)
{
    private Guid _userId;

    private async Task<Event> ArrangeEventAsync(int seats = 10)
    {
        var evt = PostgreSqlFixture.NewEvent(seats: seats);
        var user = new User($"user-{Guid.NewGuid():N}", "hash", UserRoleEnum.User);
        _userId = user.Id;
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Events.Add(evt);
        arrangeContext.Users.Add(user);
        await arrangeContext.SaveChangesAsync();
        return evt;
    }

    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id, _userId);

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        await repository.AddAsync(booking, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        saved.EventId.Should().Be(evt.Id);
        saved.Status.Should().Be(BookingStatusEnum.Pending);
        saved.ProcessedAt.Should().BeNull();
        saved.CreatedAt.Should().BeCloseTo(booking.CreatedAt, PostgreSqlFixture.DatePrecision);
    }

    [Fact]
    public async Task AddAsync_PersistsBookingAndReservedSeats_WhenEventIsTracked()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync(seats: 4);

        await using var context = fixture.CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);
        var tracked = await eventRepository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);
        tracked!.TryReserveSeats().Should().BeTrue();
        var booking = new Booking(evt.Id, _userId);

        // Act
        await bookingRepository.AddAsync(booking, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var savedBooking = await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        var savedEvent = await verify.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        savedBooking.EventId.Should().Be(evt.Id);
        savedEvent.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id, _userId);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(booking.Id);
        loaded.EventId.Should().Be(evt.Id);
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
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id, _userId);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        var tracked = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);
        tracked!.Confirm();

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
    public async Task UpdateAsync_PersistsRejectedStatusAndReleasedSeats_WhenEventIsTracked()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync(seats: 2);

        await using var context = fixture.CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);
        var trackedEvent = await eventRepository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);
        trackedEvent!.TryReserveSeats();
        var booking = new Booking(evt.Id, _userId);
        await bookingRepository.AddAsync(booking, TestContext.Current.CancellationToken);

        var trackedBooking = await bookingRepository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);
        trackedBooking!.Reject();
        trackedEvent.ReleaseSeats();

        // Act
        await bookingRepository.UpdateAsync(trackedBooking, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        var savedBooking = await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        var savedEvent = await verify.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        savedBooking.Status.Should().Be(BookingStatusEnum.Rejected);
        savedBooking.ProcessedAt.Should().NotBeNull();
        savedEvent.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPendingOrderedByCreatedAt()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var first = new Booking(evt.Id, _userId);
        var second = new Booking(evt.Id, _userId);
        var confirmed = new Booking(evt.Id, _userId);
        confirmed.Confirm();

        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(first);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await Task.Delay(20, TestContext.Current.CancellationToken);
            arrangeContext.Bookings.Add(second);
            arrangeContext.Bookings.Add(confirmed);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batch: 50, TestContext.Current.CancellationToken);

        // Assert
        pending.Should().Equal(first.Id, second.Id);
        pending.Should().NotContain(confirmed.Id);
    }

    [Fact]
    public async Task GetPendingIdsAsync_RespectsBatchSize()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.AddRange(new Booking(evt.Id, _userId), new Booking(evt.Id, _userId), new Booking(evt.Id, _userId));
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batch: 2, TestContext.Current.CancellationToken);

        // Assert
        pending.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserIdAsync_AndGetByIdAsync_RestrictToOwner()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id, _userId);
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        await repository.AddAsync(booking, TestContext.Current.CancellationToken);

        // Act
        var own = await repository.GetByUserIdAsync(_userId, TestContext.Current.CancellationToken);
        var another = await repository.GetByIdAsync(booking.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var ownerBooking = await repository.GetByIdAsync(booking.Id, _userId, TestContext.Current.CancellationToken);

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
        var evt = await ArrangeEventAsync();
        var active = new Booking(evt.Id, _userId);
        var cancelled = new Booking(evt.Id, _userId);
        cancelled.Confirm();
        cancelled.Cancel();
        await using var context = fixture.CreateContext();
        context.Bookings.AddRange(active, cancelled);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new BookingRepository(context);

        // Act
        var count = await repository.CountUserBookings(_userId, TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_PersistsCancelledBookingAndReleasedSeat_WhenEventIsTracked()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var totalSeats = 2;
        var evt = await ArrangeEventAsync(seats: totalSeats);
        var booking = new Booking(evt.Id, _userId);
        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        var trackedEvent = await context.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        trackedEvent.TryReserveSeats().Should().BeTrue();
        await repository.AddAsync(booking, TestContext.Current.CancellationToken);
        booking.Confirm();
        booking.Cancel();
        trackedEvent.ReleaseSeats();

        // Act
        await repository.UpdateAsync(booking, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        (await verify.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken)).Status.Should().Be(BookingStatusEnum.Cancelled);
        (await verify.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken)).AvailableSeats.Should().Be(totalSeats);
    }
}

using EventPlatform.Api.Model;
using EventPlatform.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.IntegrationTests;

public class BookingRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private async Task<Event> ArrangeEventAsync(int seats = 10)
    {
        var evt = PostgreSqlFixture.NewEvent(seats: seats);
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Events.Add(evt);
        await arrangeContext.SaveChangesAsync();
        return evt;
    }

    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        await repository.AddAsync(booking);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
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
        var tracked = await eventRepository.GetByIdAsync(evt.Id);
        tracked!.TryReserveSeats().Should().BeTrue();
        var booking = new Booking(evt.Id);

        // Act
        await bookingRepository.AddAsync(booking);

        // Assert
        await using var verify = fixture.CreateContext();
        var savedBooking = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
        var savedEvent = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        savedBooking.EventId.Should().Be(evt.Id);
        savedEvent.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var loaded = await repository.GetByIdAsync(booking.Id);

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
        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsConfirmedStatus()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);
        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);
        var tracked = await repository.GetByIdAsync(booking.Id);
        tracked!.Confirm();

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = fixture.CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
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
        var trackedEvent = await eventRepository.GetByIdAsync(evt.Id);
        trackedEvent!.TryReserveSeats();
        var booking = new Booking(evt.Id);
        await bookingRepository.AddAsync(booking);

        var trackedBooking = await bookingRepository.GetByIdAsync(booking.Id);
        trackedBooking!.Reject();
        trackedEvent.ReleaseSeats();

        // Act
        await bookingRepository.UpdateAsync(trackedBooking);

        // Assert
        await using var verify = fixture.CreateContext();
        var savedBooking = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
        var savedEvent = await verify.Events.SingleAsync(e => e.Id == evt.Id);
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
        var first = new Booking(evt.Id);
        var second = new Booking(evt.Id);
        var confirmed = new Booking(evt.Id);
        confirmed.Confirm();

        await using (var arrangeContext = fixture.CreateContext())
        {
            arrangeContext.Bookings.Add(first);
            await arrangeContext.SaveChangesAsync();
            await Task.Delay(20);
            arrangeContext.Bookings.Add(second);
            arrangeContext.Bookings.Add(confirmed);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batch: 50);

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
            arrangeContext.Bookings.AddRange(new Booking(evt.Id), new Booking(evt.Id), new Booking(evt.Id));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batch: 2);

        // Assert
        pending.Should().HaveCount(2);
    }
}

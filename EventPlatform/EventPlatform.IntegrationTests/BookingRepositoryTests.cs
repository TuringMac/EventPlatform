using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Model;
using EventPlatform.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventPlatform.IntegrationTests;

public class BookingRepositoryTests : IAsyncLifetime
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

    private async Task<Event> ArrangeEventAsync(int seats = 10)
    {
        var evt = NewEvent(seats: seats);
        await using var arrangeContext = CreateContext();
        arrangeContext.Events.Add(evt);
        await arrangeContext.SaveChangesAsync();
        return evt;
    }

    #endregion Infrastructure

    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);

        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        // Act
        await repository.AddAsync(booking);

        // Assert
        await using var verify = CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
        saved.EventId.Should().Be(evt.Id);
        saved.Status.Should().Be(BookingStatusEnum.Pending);
        saved.ProcessedAt.Should().BeNull();
        saved.CreatedAt.Should().BeCloseTo(booking.CreatedAt, DatePrecision);
    }

    [Fact]
    public async Task AddAsync_PersistsBookingAndReservedSeats_WhenEventIsTracked()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync(seats: 4);

        await using var context = CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);
        var tracked = await eventRepository.GetByIdAsync(evt.Id);
        tracked!.TryReserveSeats().Should().BeTrue();
        var booking = new Booking(evt.Id);

        // Act
        await bookingRepository.AddAsync(booking);

        // Assert
        await using var verify = CreateContext();
        var savedBooking = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
        var savedEvent = await verify.Events.SingleAsync(e => e.Id == evt.Id);
        savedBooking.EventId.Should().Be(evt.Id);
        savedEvent.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
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
        await ResetDatabaseAsync();
        await using var context = CreateContext();
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
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var booking = new Booking(evt.Id);
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Bookings.Add(booking);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var tracked = await repository.GetByIdAsync(booking.Id);
        tracked!.Confirm();

        // Act
        await repository.UpdateAsync(tracked);

        // Assert
        await using var verify = CreateContext();
        var saved = await verify.Bookings.SingleAsync(b => b.Id == booking.Id);
        saved.Status.Should().Be(BookingStatusEnum.Confirmed);
        saved.ProcessedAt.Should().NotBeNull();
        saved.ProcessedAt.Should().BeCloseTo(tracked.ProcessedAt!.Value, DatePrecision);
    }

    [Fact]
    public async Task UpdateAsync_PersistsRejectedStatusAndReleasedSeats_WhenEventIsTracked()
    {
        // Arrange
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync(seats: 2);

        await using var context = CreateContext();
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
        await using var verify = CreateContext();
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
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        var first = new Booking(evt.Id);
        var second = new Booking(evt.Id);
        var confirmed = new Booking(evt.Id);
        confirmed.Confirm();

        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Bookings.Add(first);
            await arrangeContext.SaveChangesAsync();
            await Task.Delay(20);
            arrangeContext.Bookings.Add(second);
            arrangeContext.Bookings.Add(confirmed);
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
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
        await ResetDatabaseAsync();
        var evt = await ArrangeEventAsync();
        await using (var arrangeContext = CreateContext())
        {
            arrangeContext.Bookings.AddRange(new Booking(evt.Id), new Booking(evt.Id), new Booking(evt.Id));
            await arrangeContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        // Act
        var pending = await repository.GetPendingIdsAsync(batch: 2);

        // Assert
        pending.Should().HaveCount(2);
    }
}

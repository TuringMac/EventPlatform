using EventPlatform.Application.Interfaces;
using EventPlatform.Application.Services;
using EventPlatform.Domain.Exceptions;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.DbContexts;
using EventPlatform.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventPlatform.Tests;

public class BookingServiceTest
{
    private const int BookingLimit = 3;
    private readonly ServiceProvider _provider;
    private readonly AppDbContext _db;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public BookingServiceTest()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _provider = services.BuildServiceProvider();

        _db = _provider.GetRequiredService<AppDbContext>();
        _eventService = _provider.GetRequiredService<IEventService>();
        _bookingService = _provider.GetRequiredService<IBookingService>();
    }

    #region Success cases

    [Fact]
    public async Task CreateBookingForExistedEvent_ShouldBePendingAndCallAddOnce()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var book = await CreateBookingAsync(evt.Id);

        // Assert
        book.Status.Should().Be(BookingStatusEnum.Pending);
    }

    [Fact]
    public async Task CreateSomeBookingsForOneEvent_ShouldBeDifferentIds()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var booking1 = await CreateBookingAsync(evt.Id);
        var booking2 = await CreateBookingAsync(evt.Id);

        // Assert
        booking1.Id.Should().NotBe(booking2.Id);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var booking = await CreateTestBookingAsync(evt.Id);

        // Act
        var bookingGet = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingGet.Should().Be(booking);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var evt = await CreateTestEventAsync(4);
        var oldSeats = evt.AvailableSeats;

        // Act
        await CreateBookingAsync(evt.Id);
        evt = await _eventService.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);
        var newAvailableSeats = evt.AvailableSeats;

        // Assert
        newAvailableSeats.Should().Be(oldSeats - 1);
    }

    [Fact]
    public async Task CreateBookingToLimit_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var evt = await CreateTestEventAsync(3);

        // Act
        Task[] tasks = {
            CreateBookingAsync(evt.Id),
            CreateBookingAsync(evt.Id),
            CreateBookingAsync(evt.Id),
        };
        await Task.WhenAll(tasks);
        evt = await _eventService.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        evt.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingBelowLimit_ShouldThrowNoAvailableSeats()
    {
        // Arrange
        var evt = await CreateTestEventAsync(1);

        // Act
        Task[] tasks = {
            CreateBookingAsync(evt.Id),
            CreateBookingAsync(evt.Id),
        };
        var act = async () => await Task.WhenAll(tasks);

        // Assert
        await act.Should()
            .ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task ProcessBooking_ShouldBeConfirmed()
    {
        // Arrange
        var evt = await CreateTestEventAsync(4);
        var booking = await CreateBookingAsync(evt.Id);
        var oldStatus = booking.Status;

        // Act
        await _bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken);
        booking = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        oldStatus.Should().Be(BookingStatusEnum.Pending);
        booking.Status.Should().Be(BookingStatusEnum.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingReject_StatusShouldBeRejected()
    {
        // Arrange
        var evt = await CreateTestEventAsync(1);
        var booking = await CreateBookingAsync(evt.Id);
        var statusBefore = booking.Status;
        evt.StartAt = DateTime.UtcNow.AddHours(-1);
        evt.EndAt = evt.StartAt.AddSeconds(10);
        await _eventService.UpdateAsync(evt.Id, evt, TestContext.Current.CancellationToken);

        // Act
        await _bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken);
        booking = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        statusBefore.Should().Be(BookingStatusEnum.Pending);
        booking.Status.Should().Be(BookingStatusEnum.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingReject_SeatsShouldBeReleased()
    {
        // Arrange
        var totalSeats = 1;
        var evt = await CreateTestEventAsync(totalSeats);
        var oldAvailableSeats = evt.AvailableSeats;

        // Act
        // Создаем бронь - резервируется место
        var booking = await CreateBookingAsync(evt.Id);
        var midAvailableSeats = evt.AvailableSeats;
        // Отклоняем бронь - место освобождается
        evt.StartAt = DateTime.UtcNow.AddHours(-1);
        evt.EndAt = evt.StartAt.AddSeconds(10);
        await _eventService.UpdateAsync(evt.Id, evt, TestContext.Current.CancellationToken);
        // Событие уже закончилось
        await _bookingService.ProcessBookingAsync(booking.Id, TestContext.Current.CancellationToken);
        var newAvailableSeats = evt.AvailableSeats;
        evt.EndAt = DateTime.UtcNow.AddDays(1);
        await _eventService.UpdateAsync(evt.Id, evt, TestContext.Current.CancellationToken);
        // Можно снова создать бронь без исключения
        booking = await CreateBookingAsync(evt.Id);

        // Assert
        oldAvailableSeats.Should().Be(totalSeats);
        midAvailableSeats.Should().Be(totalSeats - 1);
        newAvailableSeats.Should().Be(totalSeats);
    }

    [Fact]
    public async Task UniqueConcurrencyTest_UniqueIds()
    {
        // Arrange
        var requestAmount = 10;
        var totalSeats = 10;
        var evt = await CreateTestEventAsync(totalSeats);
        var set = new HashSet<Guid>();

        // Act
        var tasks = Enumerable.Range(0, requestAmount).Select(_ => CreateBookingAsync(evt.Id));
        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Select(b => b.Id).All(set.Add).Should().BeTrue();
    }

    #endregion Success cases

    #region Fail cases

    [Fact]
    public async Task CreateBookingForNotExistedEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        var notexistedEventId = Guid.NewGuid();

        // Act
        var act = async () => await CreateBookingAsync(notexistedEventId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetBookingByNotExistingId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var notExistingBookingId = Guid.NewGuid();

        // Act
        var act = async () => await _bookingService.GetBookingByIdAsync(notExistingBookingId, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    [Fact]
    public async Task ConcurentOverbooking_5success15Rejects()
    {
        // Arrange
        var requestAmount = 20;
        var totalSeats = 5;
        var evt = await CreateTestEventAsync(totalSeats);

        // Act
        var tasks = Enumerable.Range(0, requestAmount).Select(_ => CreateBookingAsync(evt.Id)).ToList();
        var tasksDone = Task.WhenAll(tasks);
        try
        {
            await tasksDone;
        }
        catch (NoAvailableSeatsException)
        {
        }

        // Assert
        tasksDone.Exception.Should().NotBeNull();
        tasksDone.Exception!.Flatten().InnerExceptions.Count.Should().Be(requestAmount - totalSeats);
        tasks.Count(t => t.IsCompletedSuccessfully).Should().Be(totalSeats);
    }

    #endregion Fail cases

    [Fact]
    public async Task CreateBooking_WhenUserReachesLimit_ThrowsWithoutReservingSeat()
    {
        // Arrange
        var seats = 5;
        var userId = Guid.NewGuid();
        var evt = await CreateTestEventAsync(seats);
        for (var i = 0; i < BookingLimit; i++)
            await _bookingService.CreateBookingAsync(evt.Id, userId, BookingLimit, TestContext.Current.CancellationToken);

        // Act
        var act = () => _bookingService.CreateBookingAsync(evt.Id, userId, BookingLimit, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<BookingLimitReachedException>();
        evt.AvailableSeats.Should().Be(seats - BookingLimit);
    }

    [Fact]
    public async Task CreateBooking_WhenEventEnded_ThrowsEventEndedException()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        evt.EndAt = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var act = () => CreateBookingAsync(evt.Id);

        // Assert
        await act.Should().ThrowAsync<EventEndedException>();
    }

    [Fact]
    public async Task GetBookingById_WhenDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var booking = await CreateBookingAsync(evt.Id);

        // Act
        var act = () => _bookingService.GetBookingByIdAsync(booking.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await _bookingService.GetBookingByIdAsync(booking.Id, booking.UserId, TestContext.Current.CancellationToken))
            .Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task CancelBookingById_WhenConfirmed_ChangesStatus()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var booking = await CreateBookingAsync(evt.Id);
        booking.Confirm();
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var cancelled = await _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        cancelled.Status.Should().Be(BookingStatusEnum.Cancelled);
        cancelled.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelBookingById_WhenPending_ChangesStatus()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var booking = await CreateBookingAsync(evt.Id);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var cancelled = await _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        cancelled.Status.Should().Be(BookingStatusEnum.Cancelled);
        cancelled.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelBookingById_WhenAlreadyCancelled_ThrowsWithoutReleasingAnotherSeat()
    {
        // Arrange
        var totalSeats = 2;
        var evt = await CreateTestEventAsync(totalSeats);
        var booking = await CreateBookingAsync(evt.Id);
        var seatsBeforeCancellation = evt.AvailableSeats;
        await _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);
        var seatsAfterCancellation = evt.AvailableSeats;

        // Act
        var act = () => _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        seatsBeforeCancellation.Should().Be(totalSeats - 1);
        seatsAfterCancellation.Should().Be(totalSeats);
        evt.AvailableSeats.Should().Be(seatsAfterCancellation);
    }

    [Fact]
    public async Task CancelBookingById_WhenRejected_ThrowsWithoutReleasingSeat()
    {
        // Arrange
        var totalSeats = 2;
        var evt = await CreateTestEventAsync(totalSeats);
        var booking = await CreateBookingAsync(evt.Id);
        booking.Reject();
        evt.ReleaseSeats();
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var seatsBeforeCancellation = evt.AvailableSeats;

        // Act
        var act = () => _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusEnum.Rejected);
        evt.AvailableSeats.Should().Be(seatsBeforeCancellation);
        evt.AvailableSeats.Should().Be(totalSeats);
    }

    [Fact]
    public async Task CancelBookingById_WhenEventEnded_ThrowsWithoutReleasingSeat()
    {
        // Arrange
        var totalSeats = 2;
        var evt = await CreateTestEventAsync(totalSeats);
        var booking = await CreateBookingAsync(evt.Id);
        evt.EndAt = DateTime.UtcNow.AddMinutes(-1);
        var seatsBeforeCancellation = evt.AvailableSeats;

        // Act
        var act = () => _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EventEndedException>();
        booking.Status.Should().Be(BookingStatusEnum.Pending);
        evt.AvailableSeats.Should().Be(seatsBeforeCancellation);
        evt.AvailableSeats.Should().Be(totalSeats - 1);
    }

    [Fact]
    public async Task CancelBookingAsync_FindsBookingForUserAndEvent()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var booking = await CreateBookingAsync(evt.Id);
        booking.Confirm();
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var cancelled = await _bookingService.CancelBookingAsync(evt.Id, booking.UserId, TestContext.Current.CancellationToken);

        // Assert
        cancelled.Id.Should().Be(booking.Id);
        cancelled.Status.Should().Be(BookingStatusEnum.Cancelled);
    }

    [Fact]
    public async Task GetBookingsByUserIdAsync_ReturnsOnlyUsersBookings()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var own = await CreateBookingAsync(evt.Id);
        await CreateBookingAsync(evt.Id);

        // Act
        var bookings = await _bookingService.GetBookingsByUserIdAsync(own.UserId, TestContext.Current.CancellationToken);

        // Assert
        bookings.Should().ContainSingle().Which.Id.Should().Be(own.Id);
    }

    async Task<Event> CreateTestEventAsync(int totalSeats = 0)
    {
        return await _eventService.CreateEventAsync(
                Guid.NewGuid(),
                "Test event Title",
                "Test event Description",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(3),
                totalSeats > 0 ? totalSeats : new Random().Next(3, 8),
                TestContext.Current.CancellationToken
            );
    }

    Task<Booking> CreateBookingAsync(Guid eventId) =>
        _bookingService.CreateBookingAsync(eventId, Guid.NewGuid(), BookingLimit, TestContext.Current.CancellationToken);

    async Task<Booking> CreateTestBookingAsync(Guid eventId)
    {
        var booking = await CreateBookingAsync(eventId);
        // booking.Status = BookingStatusEnum.Confirmed;
        return booking;
    }
}

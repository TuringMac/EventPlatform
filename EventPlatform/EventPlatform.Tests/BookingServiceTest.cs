using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Exceptions;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using EventPlatform.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventPlatform.Tests;

public class BookingServiceTest
{
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
        var book = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        book.Status.Should().Be(BookingStatusEnum.Pending);
    }

    [Fact]
    public async Task CreateSomeBookingsForOneEvent_ShouldBeDifferentIds()
    {
        // Arrange
        var evt = await CreateTestEventAsync();

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
        var booking2 = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);

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
        booking.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var evt = await CreateTestEventAsync(4);
        var oldSeats = evt.AvailableSeats;

        // Act
        await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
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

        // Acts
        Task[] tasks = {
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
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

        // Acts
        Task[] tasks = {
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
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
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
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
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
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
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
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
        booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);

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
        List<Task<Booking>> tasks = new();
        for (int i = 0; i < requestAmount; i++)
        {
            tasks.Add(_bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken));
        }
        var tasksDone = Task.WhenAll(tasks);
        var set = new HashSet<Guid>();

        // Act
        var bookings = await tasksDone;

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
        var act = async () => await _bookingService.CreateBookingAsync(notexistedEventId, TestContext.Current.CancellationToken);

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
        List<Task<Booking>> tasks = new();
        for (int i = 0; i < requestAmount; i++)
        {
            tasks.Add(_bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken));
        }
        var tasksDone = Task.WhenAll(tasks);

        // Act
        try
        {
            await tasksDone;
        }
        catch
        {
            // Assert
            tasksDone.Exception!.Flatten().InnerExceptions.Count.Should().Be(requestAmount - totalSeats);
            tasks.Where(t => t.IsCompletedSuccessfully).Count().Should().Be(totalSeats);
            //.Length.Should().Be(totalSeats);
        }
    }

    #endregion Fail cases

    async Task<Event> CreateTestEventAsync(int totalSeats = 0)
    {
        return await _eventService.CreateEventAsync(
                Guid.NewGuid(),
                "Test event Title",
                "Test event Description",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(3),
                totalSeats > 0 ? totalSeats : new Random().Next(3, 8)
            );
    }

    async Task<Booking> CreateTestBookingAsync(Guid eventId)
    {
        var booking = await _bookingService.CreateBookingAsync(eventId);
        // booking.Status = BookingStatusEnum.Confirmed;
        return booking;
    }
}

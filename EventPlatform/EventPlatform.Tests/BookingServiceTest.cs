using EventPlatform.Api.Exceptions;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using EventPlatform.Api.Services;
using EventPlatform.Api.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventPlatform.Tests;

public class BookingServiceTest
{
    readonly List<Event> _events = new();
    readonly List<Booking> _bookings = new();
    readonly Guid existedEventId;
    readonly Guid confirmedBookingId;

    readonly Mock<IBookingStorage> _bookingStorage;
    readonly Mock<IEventService> _eventService;
    readonly IBookingService _bookingService;

    public BookingServiceTest()
    {
        #region Test data

        Event existedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Тестовое событие",
            StartAt = DateTime.Parse("2026-06-30 12:00"),
            EndAt = DateTime.Parse("2026-07-01 13:00"),
            TotalSeats = 5,
        };
        existedEventId = existedEvent.Id;
        _events.Add(existedEvent);

        Booking existedBooking = new Booking
        {
            EventId = existedEventId,
        };
        confirmedBookingId = existedBooking.Id;
        existedBooking.Confirm();
        _bookings.Add(existedBooking);

        #endregion Test data

        _eventService = new Mock<IEventService>();
        _eventService
            .Setup(service => service.GetById(It.IsAny<Guid>()))
            .Returns<Guid>(id =>
            {
                var evt = _events.FirstOrDefault(e => e.Id == id);
                if (evt == null)
                    throw new KeyNotFoundException($"Событие с ID {id} не найдено");
                return evt;
            });
        _eventService.Setup(s => s.Update(existedEvent.Id, It.IsAny<Event>()));
        _bookingStorage = new Mock<IBookingStorage>();
        _bookingStorage
            .Setup(storage => storage.Add(It.IsAny<Booking>()))
            .Callback<Booking>(booking =>
            {
                if (!_events.Select(e => e.Id).Contains(booking.EventId))
                    throw new KeyNotFoundException($"Событие с ID {booking.EventId} не найдено");
                _bookings.Add(booking);
            });
        _bookingStorage
            .Setup(storage => storage.GetById(It.IsAny<Guid>()))
            .Returns((Guid id) =>
            {
                var booking = _bookings.SingleOrDefault(b => b.Id == id);
                if (booking != null)
                    return booking;
                else
                    throw new KeyNotFoundException();
            });
        _bookingService = new BookingService(_bookingStorage.Object, _eventService.Object, new LoggerFactory().CreateLogger<BookingService>());
    }

    #region Success cases

    [Fact]
    public async Task CreateBookingForExistedEvent_ShouldBePendingAndCallAddOnce()
    {
        // Arrange

        // Act
        var book = await _bookingService.CreateBookingAsync(existedEventId, TestContext.Current.CancellationToken);

        // Assert
        book.Status.Should().Be(BookingStatusEnum.Pending);
        _bookingStorage.Verify(storage => storage.Add(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateSomeBookingsForOneEvent_ShouldBeDifferentIds()
    {
        // Arrange
        // Act
        var book1 = await _bookingService.CreateBookingAsync(existedEventId, TestContext.Current.CancellationToken);
        var book2 = await _bookingService.CreateBookingAsync(existedEventId, TestContext.Current.CancellationToken);
        // Assert
        book1.Id.Should().NotBe(book2.Id);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking()
    {
        // Arrange
        // Act
        var booking = await _bookingService.GetBookingByIdAsync(confirmedBookingId, TestContext.Current.CancellationToken);
        // Assert
        booking.Should().BeEquivalentTo(_bookings.Single(b => b.Id == confirmedBookingId));
    }

    [Fact]
    public async Task BookingChangesState()
    {
        // Arrange
        var eventId = existedEventId;

        // Act
        var bookBefore = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var statusBefore = bookBefore.Status;
        bookBefore.Confirm();
        var bookAfter = await _bookingService.GetBookingByIdAsync(bookBefore.Id, TestContext.Current.CancellationToken);
        var statusAfter = bookAfter.Status;

        // Assert
        statusBefore.Should().Be(BookingStatusEnum.Pending);
        statusAfter.Should().Be(BookingStatusEnum.Confirmed);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var evt = CreateTestEvent(4);
        _events.Add(evt);
        var oldSeats = evt.AvailableSeats;

        // Act
        await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        evt.AvailableSeats.Should().Be(oldSeats - 1);
    }

    [Fact]
    public async Task CreateBookingToLimit_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var evt = CreateTestEvent(3);
        _events.Add(evt);

        // Acts
        Task[] tasks = {
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
        };
        await Task.WhenAll(tasks);

        // Assert
        evt.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingBelowLimit_ShouldThrowNoAvailableSeats()
    {
        // Arrange
        var evt = CreateTestEvent(1);
        _events.Add(evt);

        // Acts
        Task[] tasks = {
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
            _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken),
        };
        var act = async () => await Task.WhenAll(tasks);

        // Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(act);
    }

    [Fact]
    public async Task ProcessBooking_ShouldBeConfirmed()
    {
        // Arrange
        var evt = CreateTestEvent(4);
        _events.Add(evt);
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);

        // Act
        await _bookingService.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        // Assert
        booking.Status.Should().Be(BookingStatusEnum.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingConfirm_StatusShouldBeConfirmed()
    {
        // Arrange
        var evt = CreateTestEvent(1);
        _events.Add(evt);

        // Act
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatusEnum.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingReject_StatusShouldBeRejected()
    {
        // Arrange
        var evt = CreateTestEvent(1);
        _events.Add(evt);

        // Act
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatusEnum.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingReject_SeatsShouldBeReleased()
    {
        // Arrange
        var totalSeats = 1;
        var evt = CreateTestEvent(totalSeats);
        _events.Add(evt);
        var oldAvailableSeats = evt.AvailableSeats;

        // Act
        // Создаем бронь - резервируется место
        var booking = await _bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken);
        var midAvailableSeats = evt.AvailableSeats;
        // Отклоняем бронь - место освобождается
        booking.Reject();
        evt.ReleaseSeats();
        var newAvailableSeats = evt.AvailableSeats;
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
        var evt = CreateTestEvent(totalSeats);
        _events.Add(evt);
        List<Task<Booking>> tasks = new();
        for (int i = 0; i < requestAmount; i++)
        {
            tasks.Add(_bookingService.CreateBookingAsync(evt.Id, TestContext.Current.CancellationToken));
        }
        var set = new HashSet<Guid>();

        // Act
        var tasksDone = Task.WhenAll(tasks);
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
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    [Fact]
    public async Task CreateBookingForDeletedEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        var deletedEventId = Guid.NewGuid();

        // Act
        var act = async () => await _bookingService.CreateBookingAsync(deletedEventId, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
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
        var evt = CreateTestEvent(totalSeats);
        _events.Add(evt);
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
            tasks.Where(t=>t.IsCompletedSuccessfully).Count().Should().Be(totalSeats);
                //.Length.Should().Be(totalSeats);
        }
    }

    #endregion Fail cases

    Event CreateTestEvent(int totalSeats)
    {
        return new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = totalSeats,
        };
    }
}

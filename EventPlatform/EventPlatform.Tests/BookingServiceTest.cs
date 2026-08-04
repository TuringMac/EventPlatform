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
    readonly Event existedEvent = new Event
    {
        Id = new Guid("B6780633-10B5-40DF-B129-B7A01D6F7EE6"),
        Title = "Тестовое событие",
        StartAt = DateTime.Parse("2026-06-30 12:00"),
        EndAt = DateTime.Parse("2026-07-01 13:00"),
        TotalSeats = 5,
    };
    readonly Booking existedBooking = new Booking
    {
        EventId = new Guid("B6780633-10B5-40DF-B129-B7A01D6F7EE6"),
        ProcessedAt = DateTime.UtcNow
    };
    Booking? newBooking = null;
    readonly Mock<IBookingStorage> _bookingStorage;
    readonly Mock<IEventService> _eventService;
    readonly IBookingService _bookingService;

    public BookingServiceTest()
    {
        existedBooking.Confirm();
        _eventService = new Mock<IEventService>();
        _eventService
            .Setup(s => s.GetById(existedEvent.Id))
            .Returns(existedEvent);
        _eventService.Setup(service => service.GetById(It.Is<Guid>(id => id != existedEvent.Id)))
            .Throws((Guid id) => new KeyNotFoundException($"Событие с ID {id} не найдено"));
        _eventService.Setup(s => s.Update(existedEvent.Id, It.IsAny<Event>()));
        _bookingStorage = new Mock<IBookingStorage>();
        _bookingStorage.Setup(storage => storage.Add(It.IsAny<Booking>())).Callback<Booking>(booking =>
            {
                if (booking.EventId != existedEvent.Id)
                    throw new KeyNotFoundException($"Событие с ID {booking.EventId} не найдено");
                newBooking = booking;
            });
        _bookingStorage.Setup(storage => storage.GetById(It.IsAny<Guid>()))
            .Returns((Guid id) =>
            {
                if (existedBooking.Id == id)
                    return existedBooking;
                else if (newBooking?.Id == id)
                    return newBooking;
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
        var book = await _bookingService.CreateBookingAsync(existedEvent.Id, TestContext.Current.CancellationToken);

        // Assert
        book.Status.Should().Be(BookingStatusEnum.Pending);
        _bookingStorage.Verify(storage => storage.Add(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateSomeBookingsForOneEvent_ShouldBeDifferentIds()
    {
        // Arrange
        // Act
        var book1 = await _bookingService.CreateBookingAsync(existedEvent.Id, TestContext.Current.CancellationToken);
        var book2 = await _bookingService.CreateBookingAsync(existedEvent.Id, TestContext.Current.CancellationToken);
        // Assert
        book1.Id.Should().NotBe(book2.Id);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking()
    {
        // Arrange
        // Act
        var booking = await _bookingService.GetBookingByIdAsync(existedBooking.Id, TestContext.Current.CancellationToken);
        // Assert
        booking.Should().BeEquivalentTo(existedBooking);
    }

    [Fact]
    public async Task BookingChangesState()
    {
        // Arrange
        var eventId = existedEvent.Id;

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

    #endregion Fail cases
}

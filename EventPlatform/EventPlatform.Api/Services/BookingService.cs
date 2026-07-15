using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingService(IBookingStorage _bookingStorage, IEventService _eventService, ILogger<BookingService> _logger) : IBookingService
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(nameof(eventId));
        _eventService.GetById(eventId); // Выбросит исключение, если событие не найдено

        var booking = new Booking
        {
            EventId = eventId
        };
        _bookingStorage.Add(booking);
        _logger.LogInformation("Booking запрос сохранен в БД");
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId));
        return _bookingStorage.GetById(bookingId);
    }

    public async Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(nameof(id));
        var book = _bookingStorage.GetById(id);
        if (book != null && book.Status == BookingStatusEnum.Pending)
        {
            book.Status = BookingStatusEnum.Confirmed;
            book.ProcessedAt = DateTime.UtcNow;
            _bookingStorage.Update(id, book);
            _logger.LogInformation("Booking запрос {bookId} подтвержден", book.Id);
        }
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        return _bookingStorage.GetAll().Where(b => b.Status == BookingStatusEnum.Pending);
    }
}

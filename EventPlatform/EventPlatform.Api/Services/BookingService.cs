using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingService(IBookingStorage _bookingStorage, IEventService _eventService, ILogger<BookingService> _logger) : IBookingService
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentNullException(nameof(eventId));
        _eventService.GetById(eventId); // Выбросит исключение, если событие не найдено

        var booking = new Booking
        {
            EventId = eventId
        };
        _bookingStorage.Add(booking);
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId));
        return _bookingStorage.GetById(bookingId);
    }

    public async Task ConfirmAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (booking.Status == BookingStatusEnum.Pending)
        {
            booking.Status = BookingStatusEnum.Confirmed;
            booking.ProcessedAt = DateTime.UtcNow;
        }
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        return _bookingStorage.GetAll().Where(b => b.Status == BookingStatusEnum.Pending).ToList();
    }
}

using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingService(IBookingStorage _bookingStorage) : IBookingService
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var booking = new Booking
        {
            EventId = eventId
        };
        _bookingStorage.Add(booking);
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = _bookingStorage.GetById(bookingId);
        if (booking == null)
        {
            throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");
        }
        return booking;
    }
}

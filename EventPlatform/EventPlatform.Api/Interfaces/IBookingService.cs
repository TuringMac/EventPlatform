using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}

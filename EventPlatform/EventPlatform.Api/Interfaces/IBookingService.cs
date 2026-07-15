using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default);
}

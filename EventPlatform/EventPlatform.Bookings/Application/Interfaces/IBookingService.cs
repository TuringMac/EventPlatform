using EventPlatform.Bookings.Domain.Model;

namespace EventPlatform.Bookings.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Booking> CancelBookingAsync(Guid eventId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<Booking> CancelBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task RejectBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken, int batch = 50);
}

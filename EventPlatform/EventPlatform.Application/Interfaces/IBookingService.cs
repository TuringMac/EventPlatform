using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Booking> CancelBookingAsync(Guid eventId, Guid userId, UserRoleEnum userRole, CancellationToken cancellationToken);
    Task<Booking> CancelBookingByIdAsync(Guid bookingId, Guid userId, UserRoleEnum userRole, CancellationToken cancellationToken);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken, int batch = 50);
    Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken);
}

using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken = default, int batch = 50);
    Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken = default);
}

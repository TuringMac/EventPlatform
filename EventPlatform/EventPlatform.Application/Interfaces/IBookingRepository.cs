using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batch, CancellationToken cancellationToken);
    Task<Guid> GetBookingIdByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Booking> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task UpdateAsync(Booking booking, CancellationToken cancellationToken);
}

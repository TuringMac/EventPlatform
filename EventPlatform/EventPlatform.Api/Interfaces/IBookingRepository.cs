using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batch, CancellationToken cancellationToken = default);
    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
}

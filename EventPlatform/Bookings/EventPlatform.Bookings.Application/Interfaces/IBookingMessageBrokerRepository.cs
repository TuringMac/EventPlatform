namespace EventPlatform.Bookings.Application.Interfaces;

public interface IBookingMessageBrokerRepository
{
    Task CancelRequestedAsync(Guid bookingId, Guid eventId, CancellationToken cancellationToken);
    Task CreateRequestedAsync(Guid bookingId, Guid eventId, Guid userId, CancellationToken cancellationToken);
}

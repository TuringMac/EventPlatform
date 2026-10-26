using EventPlatform.Common.Application.Interfaces;

namespace EventPlatform.Events.Application.Interfaces;

public interface IEventMessageBrokerRepository : IMessageBrokerRepository
{
    Task SeatsReservationFailedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
    Task SeatsReservedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
    Task SeatsReleasedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
    Task SeatsReleaseFailedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
}

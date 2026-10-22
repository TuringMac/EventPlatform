using EventPlatform.Common.Application.Interfaces;

namespace EventPlatform.Events.Application.Interfaces;

internal interface IEventMessageBrokerRepository : IMessageBrokerRepository
{
    Task SeatsReservationFailedAsync(Guid bookingId, CancellationToken cancellationToken);
    Task SeatsReservedAsync(Guid bookingId, CancellationToken cancellationToken);
}

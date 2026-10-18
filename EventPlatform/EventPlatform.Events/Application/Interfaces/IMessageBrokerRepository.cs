namespace EventPlatform.Events.Application.Interfaces;

public interface IMessageBrokerRepository
{
    Task EventCreatedMessageAsync(Guid eventId, CancellationToken cancellationToken);
}

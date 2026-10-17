namespace EventPlatform.Users.Application.Interfaces;

public interface IMessageBrokerRepository
{
    Task UserCreatedMessageAsync(Guid userId, CancellationToken cancellationToken);
}

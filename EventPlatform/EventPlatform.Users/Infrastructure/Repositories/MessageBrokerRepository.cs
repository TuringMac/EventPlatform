using Confluent.Kafka;
using EventPlatform.Contracts;
using EventPlatform.Users.Application.Interfaces;
using EventPlatform.Users.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventPlatform.Users.Infrastructure.Repositories;

public class MessageBrokerRepository(IOptions<MessageBrokerOptions> options) : IMessageBrokerRepository
{
    public async Task UserCreatedMessageAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Implementation for sending a message to the message broker
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
        };
        using var producer = new ProducerBuilder<string, string>(config)
            .Build();
        var result = await producer.ProduceAsync(options.Value.Topic, new Message<string, string>
        {
            Key = userId.ToString(),
            Value = JsonSerializer.Serialize(new UserCreatedMessage { UserId = userId })
        }, cancellationToken);
    }
}

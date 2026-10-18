using Confluent.Kafka;
using EventPlatform.Contracts;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventPlatform.Events.Infrastructure.Repositories;

public class MessageBrokerRepository(IOptions<MessageBrokerOptions> options) : IMessageBrokerRepository
{
    public async Task EventCreatedMessageAsync(Guid eventId, CancellationToken cancellationToken)
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
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(new EventCreatedMessage { EventId = eventId })
        }, cancellationToken);
    }
}

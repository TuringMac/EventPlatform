using Confluent.Kafka;
using EventPlatform.Contracts;
using EventPlatform.Contracts.Events;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Application.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EventPlatform.Events.Infrastructure.Repositories;

public class EventMessageBrokerRepository : IEventMessageBrokerRepository, IDisposable
{
    private readonly IOptions<EventMessageBrokerOptions> _options;
    private readonly IProducer<string, string> _producer;

    public EventMessageBrokerRepository(IOptions<EventMessageBrokerOptions> options)
    {
        _options = options;
        // Implementation for sending a message to the message broker
        var config = new ProducerConfig
        {
            BootstrapServers = _options.Value.BootstrapServers,
            Acks = Acks.All,
        };
        _producer = new ProducerBuilder<string, string>(config)
            .Build();
    }

    public async Task SeatsReservationFailedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Events, new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(new EventSeatsReservationFailedMessage { BookingId = bookingId }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(EventSeatsReservationFailedMessage)) }
            }
        }, cancellationToken);
    }

    public async Task SeatsReservedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Events, new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(new EventSeatsReservedMessage { BookingId = bookingId }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(EventSeatsReservedMessage)) }
            }
        }, cancellationToken);
    }

    public async Task SeatsReleasedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Events, new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(new EventSeatsReleasedMessage { Id = eventId, BookingId = bookingId }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(EventSeatsReleasedMessage)) }
            }
        }, cancellationToken);
    }

    public async Task SeatsReleaseFailedAsync(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Events, new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(new EventSeatsReleaseFailedMessage { Id = eventId, BookingId = bookingId }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(EventSeatsReleaseFailedMessage)) }
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}

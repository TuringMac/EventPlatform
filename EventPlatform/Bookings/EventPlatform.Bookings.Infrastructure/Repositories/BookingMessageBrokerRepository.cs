using Confluent.Kafka;
using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Options;
using EventPlatform.Contracts;
using EventPlatform.Contracts.Bookings;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EventPlatform.Bookings.Infrastructure.Repositories;

public class BookingMessageBrokerRepository : IBookingMessageBrokerRepository, IDisposable
{
    private readonly IOptions<BookingMessageBrokerOptions> _options;
    private readonly IProducer<string, string> _producer;

    public BookingMessageBrokerRepository(IOptions<BookingMessageBrokerOptions> options)
    {
        this._options = options;
        // Implementation for sending a message to the message broker
        var config = new ProducerConfig
        {
            BootstrapServers = _options.Value.BootstrapServers,
            Acks = Acks.All,
        };
        _producer = new ProducerBuilder<string, string>(config)
            .Build();
    }

    public async Task CancelRequestedAsync(Guid bookingId, Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Bookings, new Message<string, string>
        {
            Key = bookingId.ToString(),
            Value = JsonSerializer.Serialize(new BookingCancelRequestedMessage
            {
                Id = bookingId,
                EventId = eventId
            }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(BookingCancelRequestedMessage)) }
            }
        }, cancellationToken);
    }

    public async Task CreateRequestedAsync(Guid bookingId, Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _producer.ProduceAsync(Topics.Bookings, new Message<string, string>
        {
            Key = bookingId.ToString(),
            Value = JsonSerializer.Serialize(new BookingCreateRequestedMessage
            {
                Id = bookingId,
                EventId = eventId
            }),
            Headers = new Headers
            {
                { _options.Value.MessageTypeHeaderName, Encoding.UTF8.GetBytes(nameof(BookingCreateRequestedMessage)) }
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}

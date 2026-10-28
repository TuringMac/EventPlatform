using EventPlatform.Common.Application.Options;

namespace EventPlatform.Bookings.Application.Options;

public class BookingMessageBrokerOptions : MessageBrokerOptions
{
    // Не каждый микросервис обязан быть консьюмером. Поэтому настройка вынесена в специфический класс опций.
    public string ConsumerGroup { get; set; } = null!;
}

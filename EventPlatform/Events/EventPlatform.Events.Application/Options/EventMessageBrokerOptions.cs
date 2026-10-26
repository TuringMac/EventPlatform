using EventPlatform.Common.Application.Options;

namespace EventPlatform.Events.Application.Options;

public class EventMessageBrokerOptions : MessageBrokerOptions
{
    // Не каждый микросервис обязан быть консьюмером. Поэтому настройка вынесена в специфический класс опций.
    public string ConsumerGroup { get; set; } = null!;
}

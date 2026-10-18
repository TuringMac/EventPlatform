namespace EventPlatform.Events.Infrastructure.Options;

public class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    public string BootstrapServers { get; set; } = null!;
    public string Topic { get; set; } = null!;
}

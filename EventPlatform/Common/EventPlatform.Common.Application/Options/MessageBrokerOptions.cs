namespace EventPlatform.Common.Application.Options;

public class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    public string BootstrapServers { get; set; } = null!;
    public string MessageTypeHeaderName { get; set; } = null!;
}

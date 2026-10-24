namespace EventPlatform.Contracts.Events;

public class EventSeatsReleasedMessage
{
    public required Guid BookingId { get; set; }
    public required DateTime ProcessedAt { get; set; }
}

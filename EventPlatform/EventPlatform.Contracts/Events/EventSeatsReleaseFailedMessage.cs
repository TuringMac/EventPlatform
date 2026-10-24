namespace EventPlatform.Contracts.Events;

public class EventSeatsReleaseFailedMessage
{
    public required Guid BookingId { get; set; }
    public required DateTime ProcessedAt { get; set; }
}

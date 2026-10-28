namespace EventPlatform.Contracts.Events;

public class EventSeatsReservedMessage
{
    public required Guid BookingId { get; set; }
    public required DateTime ProcessedAt { get; set; }
}

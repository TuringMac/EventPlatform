namespace EventPlatform.Contracts.Events;

public class EventSeatsReleasedMessage
{
    public required Guid Id { get; set; }
    public required Guid BookingId { get; set; }
}

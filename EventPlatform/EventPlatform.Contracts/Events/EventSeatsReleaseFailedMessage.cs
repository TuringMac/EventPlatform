namespace EventPlatform.Contracts.Events;

public class EventSeatsReleaseFailedMessage
{
    public required Guid Id { get; set; }
    public required Guid BookingId { get; set; }
}

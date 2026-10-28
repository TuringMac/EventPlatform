namespace EventPlatform.Contracts.Events;

public class EventSeatsReservationFailedMessage
{
    public required Guid BookingId { get; set; }
    public required DateTime ProcessedAt { get; set; }
}

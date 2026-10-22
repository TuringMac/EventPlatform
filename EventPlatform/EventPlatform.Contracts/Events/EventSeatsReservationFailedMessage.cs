namespace EventPlatform.Contracts.Events;

public class EventSeatsReservationFailedMessage
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
}

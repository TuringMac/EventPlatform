namespace EventPlatform.Contracts.Bookings;

public class BookingCancelRequestedMessage
{
    public required Guid Id { get; set; }
    public required Guid EventId { get; set; }
}

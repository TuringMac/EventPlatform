namespace EventPlatform.Contracts.Bookings;

public class BookingCreateRequestedMessage
{
    public required Guid Id { get; set; }
    public required Guid EventId { get; set; }
}

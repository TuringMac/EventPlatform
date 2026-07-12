using EventPlatform.Api.Interfaces;

namespace EventPlatform.Api.Model;

public enum BookingStatusEnum
{
    Pending,
    Confirmed,
    Rejected,
}

public class Booking : IEntity
{
    public Guid Id { get; } = Guid.NewGuid();
    public required Guid EventId { get; set; }
    public BookingStatusEnum Status { get; set; } = BookingStatusEnum.Pending;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

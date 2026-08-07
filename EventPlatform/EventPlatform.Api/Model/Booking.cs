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
    public BookingStatusEnum Status { get; private set; } = BookingStatusEnum.Pending;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public void Confirm()
    {
        if (Status == BookingStatusEnum.Pending)
        {
            Status = BookingStatusEnum.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }
        else
            throw new InvalidOperationException($"Перевести в статус {BookingStatusEnum.Confirmed} можно только из статуса {BookingStatusEnum.Pending}");
    }
    public void Reject()
    {
        if (Status == BookingStatusEnum.Pending)
        {
            Status = BookingStatusEnum.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }
        else
            throw new InvalidOperationException($"Перевести в статус {BookingStatusEnum.Rejected} можно только из статуса {BookingStatusEnum.Pending}");
    }
}

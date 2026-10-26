using EventPlatform.Bookings.Domain.Exceptions;
using EventPlatform.Common.Domain.Interfaces;

namespace EventPlatform.Bookings.Domain.Model;

public enum BookingStatusEnum
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled,
}

public class Booking : IEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public BookingStatusEnum Status { get; private set; } = BookingStatusEnum.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }

    Booking() { }

    public Booking(Guid eventId, Guid userId)
        : base()
    {
        EventId = eventId;
        UserId = userId;
    }

    public void Confirm(DateTime processedAt)
    {
        if (Status == BookingStatusEnum.Pending)
        {
            Status = BookingStatusEnum.Confirmed;
            ProcessedAt = processedAt;
        }
        else
            throw new InvalidOperationException($"Перевести в статус {BookingStatusEnum.Confirmed} можно только из статуса {BookingStatusEnum.Pending}");
    }
    public void Reject(DateTime processedAt)
    {
        if (Status == BookingStatusEnum.Pending)
        {
            Status = BookingStatusEnum.Rejected;
            ProcessedAt = processedAt;
        }
        else
            throw new InvalidOperationException($"Перевести в статус {BookingStatusEnum.Rejected} можно только из статуса {BookingStatusEnum.Pending}");
    }
    public void Cancel(DateTime processedAt)
    {
        if (Status == BookingStatusEnum.Cancelled || Status == BookingStatusEnum.Rejected)
            throw new BookingAlreadyCancelledException($"Бронь уже отменена");

        Status = BookingStatusEnum.Cancelled;
        ProcessedAt = processedAt;
    }
}

using EventPlatform.Api.Interfaces;
using System.Text.Json.Serialization;

namespace EventPlatform.Api.Model;

public enum BookingStatusEnum
{
    Pending,
    Confirmed,
    Rejected,
}

public class Booking : IEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EventId { get; private set; }
    public BookingStatusEnum Status { get; private set; } = BookingStatusEnum.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }

    [JsonIgnore]
    public Event? Event { get; set; }

    Booking() { }

    public Booking(Guid eventId)
        :base()
    {
        EventId = eventId;
    }

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

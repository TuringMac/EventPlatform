using EventPlatform.Api.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventPlatform.Api.Model;

public class Event : IEntity
{
    public Guid Id { get; init; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно для заполнения")]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    //[Range(typeof(DateTime), "2026-01-01", "2026-12-31",
    //    ErrorMessage = "Дата должна быть за 2026г.")]
    public DateTime StartAt { get; set; }
    //[Range(typeof(DateTime), "2026-01-01", "2026-12-31",
    //    ErrorMessage = "Дата должна быть за 2026г.")]
    public DateTime EndAt { get; set; }
    int _TotalSeats;
    [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше ноля")]
    public int TotalSeats
    {
        get => _TotalSeats;
        private set
        {
            _TotalSeats = value;
            _AvailableSeats = value;
        }
    }
    int _AvailableSeats;
    public int AvailableSeats
    {
        get => _AvailableSeats;
        private set => _AvailableSeats = value;
    }

    [JsonIgnore]
    public List<Booking> Bookings { get; set; } = [];

    Event() { }

    internal Event(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
        : this()
    {
        Id = id;
        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        int old;
        do
        {
            if (_AvailableSeats < count)
                return false;
            old = _AvailableSeats;
        } while (Interlocked.CompareExchange(ref this._AvailableSeats, old - count, old) != old);
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        Interlocked.Add(ref this._AvailableSeats, count);
    }
}

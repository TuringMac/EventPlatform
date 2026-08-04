using EventPlatform.Api.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Api.Model;

public class Event : IEntity
{
    public required Guid Id { get; init; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно для заполнения")]
    public required string Title { get; set; }
    public string? Description { get; set; }
    //[Range(typeof(DateTime), "2026-01-01", "2026-12-31",
    //    ErrorMessage = "Дата должна быть за 2026г.")]
    public required DateTime StartAt { get; set; }
    //[Range(typeof(DateTime), "2026-01-01", "2026-12-31",
    //    ErrorMessage = "Дата должна быть за 2026г.")]
    public required DateTime EndAt { get; set; }
    int _TotalSeats;
    [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше ноля")]
    public required int TotalSeats
    {
        get => _TotalSeats;
        init
        {
            _TotalSeats = value;
            _AvailableSeats = value;
        }
    }
    int _AvailableSeats;
    public int AvailableSeats => _AvailableSeats;

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

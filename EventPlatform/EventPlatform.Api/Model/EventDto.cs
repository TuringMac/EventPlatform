using EventPlatform.Api.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Api.Model;

public class EventDto
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
    public required int TotalSeats { get; set; }
    public int? SeatsAvailable { get; }
}

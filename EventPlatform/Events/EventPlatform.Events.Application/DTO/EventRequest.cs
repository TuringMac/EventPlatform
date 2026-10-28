using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Events.Application.DTO;

public class EventRequest
{
    public required Guid Id { get; init; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно для заполнения")]
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime StartAt { get; set; }
    public required DateTime EndAt { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше ноля")]
    public required int TotalSeats { get; set; }
}

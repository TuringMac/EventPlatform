using EventPlatform.Api.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Api.Model;

public class Event : IEntity
{
    public required Guid Id { get; init; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно для заполнения")]
    public required string Title { get; set; }
    public string Description { get; set; }
    [Range(typeof(DateTime), "2026-01-01", "2026-12-31",
        ErrorMessage = "Дата должна быть за 2026г.")]
    //public required DateTime StartAt { get; set; }
    public required DateTime StartAt
    {
        get => field;
        set
        {
            // QUESTION: Чтобы с такой проверкой вернуть стандартизированный ответ нужно отключать автоматическую проверку модели?
            if (value > EndAt && EndAt != DateTime.MinValue)
                throw new ArgumentException($"{nameof(StartAt)} не может быть позже чем {nameof(EndAt)}", nameof(StartAt));
            field = value;
        }
    }
    [Range(typeof(DateTime), "2026-01-01", "2026-12-31",
        ErrorMessage = "Дата должна быть за 2026г.")]
    //public required DateTime EndAt { get; set; }
    public required DateTime EndAt
    {
        get => field;
        set
        {
            if (value < StartAt && StartAt != DateTime.MinValue)
                throw new ArgumentException($"{nameof(EndAt)} не может быть раньше чем {nameof(StartAt)}", nameof(EndAt));
            field = value;
        }
    }
}

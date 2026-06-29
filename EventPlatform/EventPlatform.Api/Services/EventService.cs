using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class EventService(IEventStorage _context) : IEventService
{
    public void Add(Event obj)
    {
        ValidateEvent(obj);
        _context.Add(obj);
    }

    public void Delete(Guid id)
    {
        _context.Delete(id);
    }

    public PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10)
    {
        int safePage = page ?? 1;
        int safePageSize = pageSize ?? 10;

        IEnumerable<Event> events = _context.GetAll();
        // Фильтрация
        if (!string.IsNullOrWhiteSpace(title))
            events = events.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue && from > DateTime.MinValue)
            events = events.Where(e => e.StartAt >= from);
        if (to.HasValue && to < DateTime.MaxValue)
            events = events.Where(e => e.EndAt <= to);

        // Пагинация
        int totalAmount = events.Count();
        if (safePageSize < 10)
            safePageSize = 10;
        if (safePage < 1)
            safePage = 1;
        events = events.Skip((safePage - 1) * safePageSize).Take(safePageSize);

        return new PaginatedResult<Event> { Data = events, CurrentPage = safePage, PageItems = events.Count(), TotalItems = totalAmount };
    }

    public Event GetById(Guid id)
    {
        return _context.GetById(id);
    }

    public void Update(Guid id, Event obj)
    {
        ValidateEvent(obj);
        _context.Update(id, obj);
    }

    void ValidateEvent(Event obj)
    {
        if (obj.StartAt > obj.EndAt)
            throw new ArgumentException(nameof(obj.EndAt), "Дата окончания не может быть раньше даты начала.");
    }
}

using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class EventService(IEventStorage _context) : IEventService
{
    public void Add(Event obj)
    {
        if (obj.StartAt > obj.EndAt)
            throw new ArgumentOutOfRangeException(nameof(obj.EndAt), "Дата окончания не может быть раньше даты начала.");
        _context.Add(obj);
    }

    public void Delete(Guid id)
    {
        _context.Delete(id);
    }

    public IEnumerable<Event> GetAll(string? title, DateTime? from, DateTime? to)
    {
        IEnumerable<Event> events = _context.GetAll();
        if (!string.IsNullOrWhiteSpace(title))
            events = events.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from > DateTime.MinValue)
            events = events.Where(e => e.StartAt >= from);
        if (to < DateTime.MaxValue)
            events = events.Where(e => e.EndAt <= to);
        return events;
    }

    public Event GetById(Guid id)
    {
        return _context.GetById(id);
    }

    public void Update(Guid id, Event obj)
    {
        _context.Update(id, obj);
    }
}

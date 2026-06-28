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

    public IEnumerable<Event> GetAll()
    {
        return _context.GetAllEvents();
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

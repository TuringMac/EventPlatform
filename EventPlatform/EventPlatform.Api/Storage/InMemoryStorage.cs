using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Storage;

public class InMemoryStorage : IStorage
{
    List<Event> events { get; } = new List<Event>();
    public void AddEvent(Event obj)
    {
        events.Add(obj);
    }

    public bool DeleteEvent(Guid id)
    {
        var obj = events.FirstOrDefault(e => e.Id == id);
        if (obj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        return events.Remove(obj);
    }

    public IEnumerable<Event> GetAllEvents()
    {
        return events;
    }

    public Event GetEventById(Guid id)
    {
        var @event= events.FirstOrDefault(e => e.Id == id);
        if(@event==null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        return @event;
    }

    public Event UpdateEvent(Guid id, Event newObj)
    {
        var oldObj = events.FirstOrDefault(e => e.Id == id);
        if (oldObj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");

        oldObj.Title = newObj.Title;
        oldObj.Description = newObj.Description;
        oldObj.StartAt = newObj.StartAt;
        oldObj.EndAt = newObj.EndAt;
        return oldObj;
    }
}

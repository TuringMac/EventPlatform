using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Storage;

public class InMemoryStorage : IEventStorage
{
    List<Event> _events { get; } = new List<Event>();

    public InMemoryStorage()
    {
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Квадроциклы", StartAt = DateTime.Parse("2026-06-30 12:00"), EndAt = DateTime.Parse("2026-07-01 13:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Велосипеды", StartAt = DateTime.Parse("2026-07-01 11:00"), EndAt = DateTime.Parse("2026-07-01 13:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Футбол", StartAt = DateTime.Parse("2026-07-01 10:00"), EndAt = DateTime.Parse("2026-07-01 16:00") });
    }

    public void Add(Event obj)
    {
        _events.Add(obj);
    }

    public void Delete(Guid id)
    {
        var obj = _events.FirstOrDefault(e => e.Id == id);
        if (obj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        _events.Remove(obj);
    }

    public IEnumerable<Event> GetAll()
    {
        return _events;
    }

    public Event GetById(Guid id)
    {
        var @event = _events.FirstOrDefault(e => e.Id == id);
        if (@event == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        return @event;
    }

    public void Update(Guid id, Event newObj)
    {
        var oldObj = _events.FirstOrDefault(e => e.Id == id);
        if (oldObj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");

        oldObj.Title = newObj.Title;
        oldObj.Description = newObj.Description;
        oldObj.StartAt = newObj.StartAt;
        oldObj.EndAt = newObj.EndAt;
    }
}

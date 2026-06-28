using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IEventService
{
    void Add(Event obj);
    IEnumerable<Event> GetAll(string? title, DateTime? from, DateTime? to);
    Event GetById(Guid id);
    void Update(Guid id, Event obj);
}

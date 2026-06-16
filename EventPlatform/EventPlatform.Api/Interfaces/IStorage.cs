using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IStorage
{
    IEnumerable<Event> GetAllEvents();
    Event GetEventById(Guid id);
    void AddEvent(Event obj);
    Event UpdateEvent(Guid id, Event obj);
    bool DeleteEvent(Guid id);

}

using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IEventService
{
    Event CreateEventAsync(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats);
    Task Add(Event obj);
    Task<PaginatedResult<Event>> GetAll(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10);
    Task<Event> GetById(Guid id, CancellationToken cancellationToken = default);
    Task Update(Guid id, Event obj);
    Task Delete(Guid id);
}

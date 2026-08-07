using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken ct = default);
    void Add(Event obj);
    PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10);
    Event GetById(Guid id);
    void Update(Guid id, Event obj);
    void Delete(Guid id);
}

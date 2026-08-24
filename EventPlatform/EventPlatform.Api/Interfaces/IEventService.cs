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
        int totalSeats);
    Task AddAsync(Event obj, CancellationToken cancellationToken = default);
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10);
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, Event obj, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default);
}

using EventPlatform.Application.DTO;
using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken cancellationToken);
    Task AddAsync(Event obj, CancellationToken cancellationToken);
    Task<PaginatedResult<Event>> GetAllAsync(CancellationToken cancellationToken, string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10);
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, Event obj, CancellationToken cancellationToken);
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken);
}

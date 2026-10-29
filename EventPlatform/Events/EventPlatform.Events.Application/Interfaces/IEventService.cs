using EventPlatform.Events.Application.DTO;
using EventPlatform.Events.Domain.Model;

namespace EventPlatform.Events.Application.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(EventRequest eventRequest, CancellationToken cancellationToken);
    Task AddAsync(Event obj, CancellationToken cancellationToken);
    Task<PaginatedResult<Event>> GetAllAsync(CancellationToken cancellationToken, string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10);
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Event> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, EventRequest obj, CancellationToken cancellationToken);
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken);

    Task<bool> ReserveSeats(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
    Task<bool> ReleaseSeats(Guid eventId, Guid bookingId, CancellationToken cancellationToken);
    Task<IEnumerable<Event>> GetTop10EventsAsync(CancellationToken cancellationToken);
}

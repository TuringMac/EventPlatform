using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IEventRepository
{
    Task AddAsync(Event evt, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Event> events, int currentPage, int pageItems, int totalAmount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(Event evt, CancellationToken cancellationToken = default);
}

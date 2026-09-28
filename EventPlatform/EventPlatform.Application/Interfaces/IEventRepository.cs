using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IEventRepository : IRepositoryCrud<Event>
{
    Task<(IEnumerable<Event> events, int currentPage, int pageItems, int totalAmount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

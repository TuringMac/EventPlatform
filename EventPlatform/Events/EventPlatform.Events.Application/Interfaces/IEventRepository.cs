using EventPlatform.Common.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using System.Linq.Expressions;

namespace EventPlatform.Events.Application.Interfaces;

public interface IEventRepository : IRepositoryCrud<Event>
{
    Task<(IEnumerable<Event> events, int currentPage, int pageItems, int totalAmount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<IEnumerable<Event>> GetTop10EventsAsync(Expression<Func<Event, int>> keySelector, CancellationToken cancellationToken);
}

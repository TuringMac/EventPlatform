using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Api.Repositories;

public class EventRepository(AppDbContext _context) : IEventRepository
{
    public async Task AddAsync(Event evt, CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(evt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events.Where(e => e.Id == eventId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Bookings)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Event> events, int currentPage, int pageItems, int totalAmount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var eventsQuery = _context.Events.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(title))
            eventsQuery = eventsQuery.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue && from > DateTime.MinValue)
            eventsQuery = eventsQuery.Where(e => e.EndAt >= from);
        if (to.HasValue && to < DateTime.MaxValue)
            eventsQuery = eventsQuery.Where(e => e.StartAt <= to);

        var totalAmount = await eventsQuery.CountAsync(cancellationToken);
        var events = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (events, page, events.Count, totalAmount);
    }

    public async Task UpdateAsync(Event evt, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(evt).State == EntityState.Detached)
            _context.Events.Update(evt);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

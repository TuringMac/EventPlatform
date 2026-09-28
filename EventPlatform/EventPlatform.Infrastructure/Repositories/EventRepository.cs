using EventPlatform.Application.Interfaces;
using EventPlatform.Infrastructure.DbContexts;
using EventPlatform.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Infrastructure.Repositories;

public class EventRepository(AppDbContext _context) : IEventRepository
{
    public async Task AddAsync(Event evt, CancellationToken cancellationToken)
    {
        await _context.Events.AddAsync(evt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.Events.Where(e => e.Id == eventId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
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
        CancellationToken cancellationToken)
    {
        var eventsQuery = _context.Events.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(title))
            eventsQuery = eventsQuery.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));
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

    public async Task UpdateAsync(Guid id, Event evt, CancellationToken cancellationToken)
    {
        if (_context.Entry(evt).State == EntityState.Detached)
            _context.Events.Update(evt);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

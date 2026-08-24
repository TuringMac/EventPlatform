using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Api.Services;

public class EventService(AppDbContext _context, ILogger<EventService> _logger) : IEventService
{
    public async Task<Event> CreateEventAsync(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        var evt = new Event(
            id,
            title,
            startAt,
            endAt,
            totalSeats
        )
        {
            Description = description,
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task AddAsync(Event obj, CancellationToken cancellationToken = default)
    {
        ValidateEvent(obj);
        _context.Events.Add(obj);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if ((await _context.Events.Where(e => e.Id == eventId).ExecuteDeleteAsync(cancellationToken)) == 0)
            throw new KeyNotFoundException();
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10)
    {
        int safePage = page ?? 1;
        int safePageSize = pageSize ?? 10;

        if (safePage < 1)
            throw new ArgumentException("Номер страницы должен быть положительным", nameof(page));
        if (safePageSize < 1)
            throw new ArgumentException("Размер страницы должен быть положительным", nameof(pageSize));

        var eventsQuery = _context.Events.AsNoTracking();
        // Фильтрация
        if (!string.IsNullOrWhiteSpace(title))
            eventsQuery = eventsQuery.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue && from > DateTime.MinValue)
            eventsQuery = eventsQuery.Where(e => e.EndAt >= from);
        if (to.HasValue && to < DateTime.MaxValue)
            eventsQuery = eventsQuery.Where(e => e.StartAt <= to);

        // Пагинация
        var totalAmount = await eventsQuery.CountAsync();
        var events = await eventsQuery.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync();

        var pageItems = events.Count();
        _logger.LogInformation("Query filtered: {totalAmount}; Items on page {pageItems}", totalAmount, pageItems);
        return new PaginatedResult<Event> { Data = events, CurrentPage = safePage, PageItems = pageItems, TotalItems = totalAmount };
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateGuid(id);
        var evt = await _context.Events
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evt is null)
            throw new KeyNotFoundException($"Event {id} not found");
        _context.Entry(evt).Collection(o => o.Bookings).Load();
        return evt;
    }

    public async Task UpdateAsync(Guid id, Event obj, CancellationToken cancellationToken = default)
    {
        ValidateEvent(id, obj);
        var evt = await _context.Events
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evt is null)
            throw new KeyNotFoundException($"Event {id} not found");
        evt.Title = obj.Title;
        evt.Description = obj.Description;
        evt.StartAt = obj.StartAt;
        evt.EndAt = obj.EndAt;
        await _context.SaveChangesAsync();
    }

    void ValidateEvent(Event obj)
    {
        if (obj.StartAt > obj.EndAt)
            throw new ArgumentException("Дата окончания не может быть раньше даты начала.", nameof(obj.EndAt));
        if (obj.AvailableSeats > obj.TotalSeats)
            throw new ArgumentException("Количество доступных мест не может превышать общее количество мест.", nameof(obj.AvailableSeats));
        _logger.LogInformation("Event validated: {Title}, StartAt: {StartAt}, EndAt: {EndAt}", obj.Title, obj.StartAt, obj.EndAt);
    }

    void ValidateEvent(Guid id, Event obj)
    {
        ValidateGuid(id);
        if (!Equals(id, obj.Id))
            throw new ArgumentException("Id in the URL does not match Id in the body.", nameof(obj.Id));
        ValidateEvent(obj);
    }

    void ValidateGuid(Guid id)
    {
        if (Equals(id, Guid.Empty))
            throw new ArgumentException($"{nameof(id)} не может быть пустым", nameof(id));
    }
}

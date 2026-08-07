using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class EventService(IEventStorage _context, ILogger<EventService> _logger) : IEventService
{
    public async Task<Event> CreateEventAsync(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        CancellationToken ct = default)
    {
        return await Task.FromResult(new Event
        {
            Id=id, 
            Title=title, 
            Description=description,
            StartAt=startAt,
            EndAt=endAt,
            TotalSeats = totalSeats,
        });
    }

    public void Add(Event obj)
    {
        ValidateEvent(obj);
        _context.Add(obj);
    }

    public void Delete(Guid id)
    {
        ValidateGuid(id);
        _context.Delete(id);
    }

    public PaginatedResult<Event> GetAll(string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10)
    {
        int safePage = page ?? 1;
        int safePageSize = pageSize ?? 10;

        if (safePage < 1)
            throw new ArgumentException("Номер страницы должен быть положительным", nameof(page));
        if (safePageSize < 1)
            throw new ArgumentException("Размер страницы должен быть положительным", nameof(pageSize));

        IEnumerable<Event> events = _context.GetAll();
        // Фильтрация
        if (!string.IsNullOrWhiteSpace(title))
            events = events.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue && from > DateTime.MinValue)
            events = events.Where(e => e.StartAt >= from);
        if (to.HasValue && to < DateTime.MaxValue)
            events = events.Where(e => e.EndAt <= to);

        // Пагинация
        int totalAmount = events.Count();
        events = events.Skip((safePage - 1) * safePageSize).Take(safePageSize);
        var pageItems = events.Count();
        _logger.LogInformation("Query filtered: {totalAmount}; Items on page {pageItems}", totalAmount, pageItems);
        return new PaginatedResult<Event> { Data = events, CurrentPage = safePage, PageItems = pageItems, TotalItems = totalAmount };
    }

    public Event GetById(Guid id)
    {
        ValidateGuid(id);
        return _context.GetById(id);
    }

    public void Update(Guid id, Event obj)
    {
        ValidateEvent(id, obj);
        _context.Update(id, obj);
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

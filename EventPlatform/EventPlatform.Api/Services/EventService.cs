using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class EventService(IEventStorage _context, ILogger<EventService> _logger) : IEventService
{
    public void Add(Event obj)
    {
        ValidateEvent(obj);
        _context.Add(obj);
    }

    public void Delete(Guid id)
    {
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
        if (Equals(id, Guid.Empty))
            throw new ArgumentException($"{id} не может быть пустым", nameof(id));
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
        _logger.LogInformation("Event validated: {Title}, StartAt: {StartAt}, EndAt: {EndAt}", obj.Title, obj.StartAt, obj.EndAt);
    }

    void ValidateEvent(Guid id, Event obj)
    {
        if (!Equals(id, obj.Id))
            throw new ArgumentException("Id in the URL does not match Id in the body.", nameof(obj.Id));
        ValidateEvent(obj);
    }
}

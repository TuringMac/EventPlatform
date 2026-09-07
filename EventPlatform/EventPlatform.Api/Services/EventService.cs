using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class EventService(IEventRepository _eventRepository, ILogger<EventService> _logger) : IEventService
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
        await _eventRepository.AddAsync(evt);
        return evt;
    }

    public async Task AddAsync(Event obj, CancellationToken cancellationToken = default)
    {
        ValidateEvent(obj);
        await _eventRepository.AddAsync(obj, cancellationToken);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (await _eventRepository.DeleteAsync(eventId, cancellationToken) == 0)
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

        var (events, currentPage, pageItems, totalAmount) = await _eventRepository.GetPagedAsync(title, from, to, safePage, safePageSize);
        _logger.LogInformation("Query filtered: {totalAmount}; Items on page {pageItems}", totalAmount, pageItems);

        return new PaginatedResult<Event> { Data = events, CurrentPage = currentPage, PageItems = pageItems, TotalItems = totalAmount };
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateGuid(id);
        var evt = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (evt is null)
            throw new KeyNotFoundException($"Event {id} not found");
        return evt;
    }

    public async Task UpdateAsync(Guid id, Event obj, CancellationToken cancellationToken = default)
    {
        ValidateEvent(id, obj);
        var evt = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (evt is null)
            throw new KeyNotFoundException($"Event {id} not found");
        evt.Title = obj.Title;
        evt.Description = obj.Description;
        evt.StartAt = obj.StartAt;
        evt.EndAt = obj.EndAt;
        await _eventRepository.UpdateAsync(evt, cancellationToken);
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

using EventPlatform.Events.Application.DTO;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using Microsoft.Extensions.Logging;

namespace EventPlatform.Events.Application.Services;

internal class EventService(IEventRepository _eventRepository, IEventMessageBrokerRepository _messageBrokerRepository, ILogger<EventService> _logger) : IEventService
{
    public async Task<Event> CreateEventAsync(EventRequest eventRequest, CancellationToken cancellationToken)
    {
        var evt = new Event(
            Guid.NewGuid(),
            eventRequest.Title,
            eventRequest.StartAt,
            eventRequest.EndAt,
            eventRequest.TotalSeats
        )
        {
            Description = eventRequest.Description
        };
        await AddAsync(evt, cancellationToken);
        return evt;
    }

    public async Task AddAsync(Event obj, CancellationToken cancellationToken)
    {
        ValidateEvent(obj);
        await _eventRepository.AddAsync(obj, cancellationToken);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await _eventRepository.DeleteAsync(eventId, cancellationToken);
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(CancellationToken cancellationToken, string? title, DateTime? from, DateTime? to, int? page = 1, int? pageSize = 10)
    {
        int safePage = page ?? 1;
        int safePageSize = pageSize ?? 10;

        if (safePage < 1)
            throw new ArgumentException("Номер страницы должен быть положительным", nameof(page));
        if (safePageSize < 1)
            throw new ArgumentException("Размер страницы должен быть положительным", nameof(pageSize));

        var (events, currentPage, pageItems, totalAmount) = await _eventRepository.GetPagedAsync(title, from, to, safePage, safePageSize, cancellationToken);
        _logger.LogInformation("Query filtered: {totalAmount}; Items on page {pageItems}", totalAmount, pageItems);

        return new PaginatedResult<Event> { Data = events, CurrentPage = currentPage, PageItems = pageItems, TotalItems = totalAmount };
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ValidateGuid(id);
        var evt = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Мероприятие {id} не найдено");
        return evt;
    }

    public async Task UpdateAsync(Guid id, EventRequest obj, CancellationToken cancellationToken)
    {
        if (id != obj.Id)
            throw new ArgumentException("Id в URL не совпадает с Id в теле запроса.", nameof(obj.Id));
        var evt = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Мероприятие {id} не найдено");
        evt.UpdateDetails(obj.Title,
            obj.Description ?? string.Empty,
            obj.StartAt,
            obj.EndAt
        );
        ValidateEvent(id, evt);
        await _eventRepository.UpdateAsync(id, evt, cancellationToken);
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

    public async Task<bool> ReserveSeats(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        ValidateGuid(eventId);
        ValidateGuid(bookingId);

        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Мероприятие {eventId} не найдено");
        if (evt.EndAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Бронь {bookingId} отклонена, мероприятие закончилось", bookingId);
            await _messageBrokerRepository.SeatsReservationFailedAsync(eventId, bookingId, cancellationToken);
            return false;
        }
        else if (evt.AvailableSeats <= 0 || !evt.TryReserveSeats())
        {
            _logger.LogWarning("Бронь {bookingId} отклонена, мест нет", bookingId);
            await _messageBrokerRepository.SeatsReservationFailedAsync(eventId, bookingId, cancellationToken);
            return false;
        }

        await _eventRepository.UpdateAsync(eventId, evt, cancellationToken);
        await _messageBrokerRepository.SeatsReservedAsync(eventId, bookingId, cancellationToken);
        _logger.LogInformation("Места для Брони {bookingId} зарезервированы", bookingId);
        return true;
    }

    public async Task<bool> ReleaseSeats(Guid eventId, Guid bookingId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Мероприятие {eventId} не найдено");
        if (evt.EndAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Бронь {bookingId} отклонена, мероприятие закончилось", bookingId);
            await _messageBrokerRepository.SeatsReleaseFailedAsync(eventId, bookingId, cancellationToken);
            return false;
        }
        evt.ReleaseSeats();
        await _eventRepository.UpdateAsync(eventId, evt, cancellationToken);
        await _messageBrokerRepository.SeatsReleasedAsync(eventId, bookingId, cancellationToken);
        _logger.LogInformation("Места брони {bookingId} освобождены", bookingId);
        return true;
    }

    public async Task<IEnumerable<Event>> GetTop10EventsAsync(CancellationToken cancellationToken)
    {
        var top10 = await _eventRepository.GetTop10EventsAsync(e=>(e.TotalSeats - e.AvailableSeats) / e.TotalSeats, cancellationToken);
        return top10;
    }
}

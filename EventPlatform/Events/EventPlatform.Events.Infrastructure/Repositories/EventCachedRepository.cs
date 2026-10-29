using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using EventPlatform.Events.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Linq.Expressions;
using System.Text.Json;

namespace EventPlatform.Events.Infrastructure.Repositories;

/// <summary>
/// Cache-Aside для IEventRepository, Read-Through декоратор для IEventService
/// Update-on-Write (чтобы обновление кеша не выстраивало очередь в БД)
/// </summary>
/// <param name="eventRepository"></param>
/// <param name="connectionMultiplexer"></param>
/// <param name="options"></param>
/// <param name="logger"></param>
public class EventCachedRepository(
    IEventRepository eventRepository,
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<CacheOptions> options,
    ILogger<EventCachedRepository> logger) : IEventRepository
{
    #region IEventRepository

    public async Task AddAsync(Event evt, CancellationToken cancellationToken)
    {
        await eventRepository.AddAsync(evt, cancellationToken);
        await SetEventByIdAsync(evt, cancellationToken);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var evt = await eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Мероприятие с ID {eventId} не найдено.");

        await eventRepository.DeleteAsync(eventId, cancellationToken);
        await DeleteEventByIdAsync(eventId, cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cachedEvt = await GetCachedEventByIdAsync(id, cancellationToken);
        if (cachedEvt != null) // Hit
            return cachedEvt;
        // Miss
        var evt = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (evt is null)
            return null;
        // Обновить кеш
        await SetEventByIdAsync(evt, cancellationToken);

        return evt;
    }

    /// <summary>
    /// Получение мероприятия без кеша
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await eventRepository.GetForUpdateAsync(id, cancellationToken);
    }

    public async Task<(IEnumerable<Event> events, int currentPage, int pageItems, int totalAmount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return await eventRepository.GetPagedAsync(title, from, to, page, pageSize, cancellationToken);
    }

    public async Task UpdateAsync(Guid id, Event evt, CancellationToken cancellationToken)
    {
        await eventRepository.UpdateAsync(id, evt, cancellationToken);
        await SetEventByIdAsync(evt, cancellationToken);
    }

    public async Task<IEnumerable<Event>> GetTop10EventsAsync(Expression<Func<Event, int>> keySelector, CancellationToken cancellationToken)
    {
        string key = GetTop10EventsCacheKey();
        var db = connectionMultiplexer.GetDatabase();
        try
        {
            var cachedTop10 = await db.StringGetAsync(key);
            if (cachedTop10.HasValue) // Hit
                return JsonSerializer.Deserialize<IEnumerable<Event>>(cachedTop10.ToString())!;
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Ошибка чтения из кеша для ТОП10 Мероприятий KEY {top10Key}", key);
        }

        // Miss
        var top10Events = await eventRepository.GetTop10EventsAsync(keySelector, cancellationToken);
        try
        {
            string json = JsonSerializer.Serialize(top10Events);
            await db.StringSetAsync(key, json, TimeSpan.FromMinutes(options.Value.Top10TTL)); // С TTL
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Ошибка записи в кеш для ТОП10 Мероприятий KEY {top10Key}", key);
        }

        return top10Events;
    }

    #endregion IEventRepository

    private string GetEventCacheKey(Guid eventId) => string.Format("event:{0}", eventId);
    private string GetTop10EventsCacheKey() => "top10_events";

    private async Task SetEventByIdAsync(Event evt, CancellationToken cancellationToken)
    {
        var key = GetEventCacheKey(evt.Id);
        try
        {
            var db = connectionMultiplexer.GetDatabase();
            string json = JsonSerializer.Serialize(evt);
            await db.StringSetAsync(key, json, TimeSpan.FromMinutes(options.Value.RegularTTL));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Ошибка записи в кеш для Мероприятия KEY {eventIdKey}", key);
        }
    }
    private async Task<Event?> GetCachedEventByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var key = GetEventCacheKey(eventId);
        try
        {
            var db = connectionMultiplexer.GetDatabase();
            var cached = await db.StringGetAsync(key);
            if (cached.HasValue) // Hit
            {
                return JsonSerializer.Deserialize<Event>(cached.ToString());
            }
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Ошибка чтения из кеша для Мероприятия KEY {eventIdKey}", key);
        }
        return null;
    }

    /// <summary>
    /// Инвалидируем кеш для удаленного Мероприятия
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task DeleteEventByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var key = GetEventCacheKey(eventId);
        try
        {
            var db = connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Ошибка инвалидации кеша для Мероприятия KEY {eventIdKey}", key);
        }
    }
}

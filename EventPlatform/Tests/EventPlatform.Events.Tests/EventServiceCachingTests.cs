using EventPlatform.Events.Application;
using EventPlatform.Events.Application.DTO;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using EventPlatform.Events.Infrastructure.Options;
using EventPlatform.Events.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Linq.Expressions;
using System.Text.Json;

namespace EventPlatform.Events.Tests;

public class EventServiceCachingTests : IDisposable
{
    private readonly Mock<IEventRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IDatabase> _cache = new(MockBehavior.Strict);
    private readonly CacheOptions _options = new() { RegularTTL = 5, Top10TTL = 2 };
    private readonly ServiceProvider _provider;
    private readonly IEventService _service;

    public EventServiceCachingTests()
    {
        var connection = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        connection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_cache.Object);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(connection.Object);
        services.AddSingleton(Options.Create(_options));
        services.AddSingleton(Mock.Of<IEventMessageBrokerRepository>());
        services.AddSingleton<IEventRepository>(provider => new EventCachedRepository(
            _repository.Object,
            provider.GetRequiredService<IConnectionMultiplexer>(),
            provider.GetRequiredService<IOptions<CacheOptions>>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EventCachedRepository>>()));
        _provider = services.BuildServiceProvider();
        _service = _provider.GetRequiredService<IEventService>();
    }

    [Fact]
    public async Task GetById_WhenCacheHit_ReturnsCachedEventWithoutCallingRepository()
    {
        // Arrange
        var totalSeats = 10;
        var reservedSeats = 2;
        var evt = NewEvent(totalSeats);
        evt.TryReserveSeats(reservedSeats).Should().BeTrue();
        var key = GetEventCacheKey(evt.Id);
        _cache.Setup(c => c.StringGetAsync(key))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(evt));

        // Act
        var result = await _service.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(evt);
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_WhenCacheMiss_LoadsRepositoryAndCachesEventWithTtl()
    {
        // Arrange
        var totalSeats = 10;
        var evt = NewEvent(totalSeats);
        var key = GetEventCacheKey(evt.Id);
        var token = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(evt);
        var ttl = TimeSpan.FromMinutes(_options.RegularTTL);
        _cache.Setup(c => c.StringGetAsync(key)).ReturnsAsync(RedisValue.Null);
        _repository.Setup(r => r.GetByIdAsync(evt.Id, token)).ReturnsAsync(evt);
        SetupCacheWrite(key, json, ttl);

        // Act
        var result = await _service.GetByIdAsync(evt.Id, token);

        // Assert
        result.Should().BeSameAs(evt);
        _repository.Verify(r => r.GetByIdAsync(evt.Id, token), Times.Once);
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        VerifyCacheWrite(key, json, ttl);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_WhenEventMissing_DoesNotCacheAndThrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var key = GetEventCacheKey(eventId);
        var token = TestContext.Current.CancellationToken;
        _cache.Setup(c => c.StringGetAsync(key)).ReturnsAsync(RedisValue.Null);
        _repository.Setup(r => r.GetByIdAsync(eventId, token)).ReturnsAsync((Event?)null);

        // Act
        var act = () => _service.GetByIdAsync(eventId, token);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.Verify(r => r.GetByIdAsync(eventId, token), Times.Once);
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTop10_WhenCacheHit_ReturnsCachedEventsWithoutCallingRepository()
    {
        // Arrange
        var totalSeats = 10;
        Event[] events = [NewEvent(totalSeats), NewEvent(totalSeats)];
        var key = "top10_events";
        _cache.Setup(c => c.StringGetAsync(key))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(events));

        // Act
        var result = await _service.GetTop10EventsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(events, options => options.WithStrictOrdering());
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTop10_WhenCacheMiss_LoadsRepositoryAndCachesEventsWithTtl()
    {
        // Arrange
        var totalSeats = 10;
        Event[] events = [NewEvent(totalSeats), NewEvent(totalSeats)];
        var key = "top10_events";
        var token = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(events);
        var ttl = TimeSpan.FromMinutes(_options.Top10TTL);
        _cache.Setup(c => c.StringGetAsync(key)).ReturnsAsync(RedisValue.Null);
        _repository.Setup(r => r.GetTop10EventsAsync(It.IsAny<Expression<Func<Event, int>>>(), token))
            .ReturnsAsync(events);
        SetupCacheWrite(key, json, ttl);

        // Act
        var result = await _service.GetTop10EventsAsync(token);

        // Assert
        result.Should().BeSameAs(events);
        _repository.Verify(r => r.GetTop10EventsAsync(It.IsAny<Expression<Func<Event, int>>>(), token), Times.Once);
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        VerifyCacheWrite(key, json, ttl);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeSeats_UpdatesCachedAvailableSeats(bool release)
    {
        // Arrange
        var totalSeats = 10;
        var reservedSeats = 1;
        var evt = NewEvent(totalSeats);
        if (release)
            evt.TryReserveSeats(reservedSeats).Should().BeTrue();
        var expectedAvailableSeats = release ? totalSeats : totalSeats - reservedSeats;
        var key = GetEventCacheKey(evt.Id);
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var ttl = TimeSpan.FromMinutes(_options.RegularTTL);
        // Низкоуровневая настройка заглушки, чтобы убедиться, что после изменения доступных мест в кеше будет сохранено правильное состояние мероприятия
        _cache.Setup(c => c.StringGetAsync(key))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(evt));
        _repository.Setup(r => r.UpdateAsync(evt.Id, It.Is<Event>(e => e.AvailableSeats == expectedAvailableSeats), token))
            .Returns(Task.CompletedTask);
        RedisValue cachedJson = RedisValue.Null;
        _cache.Setup(c => c.StringSetAsync(key, It.IsAny<RedisValue>(), (Expiration)ttl, default))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) => cachedJson = value)
            .ReturnsAsync(true);

        // Act
        var result = release
            ? await _service.ReleaseSeats(evt.Id, bookingId, token)
            : await _service.ReserveSeats(evt.Id, bookingId, token);

        // Assert
        result.Should().BeTrue();
        var cachedEvent = JsonSerializer.Deserialize<Event>(cachedJson.ToString());
        cachedEvent.Should().BeEquivalentTo(evt, options => options.Excluding(e => e.AvailableSeats));
        cachedEvent!.AvailableSeats.Should().Be(expectedAvailableSeats);
        _repository.Verify(r => r.UpdateAsync(evt.Id, It.IsAny<Event>(), token), Times.Once);
        _cache.Verify(c => c.StringGetAsync(key), Times.Once);
        _cache.Verify(c => c.StringSetAsync(key, It.IsAny<RedisValue>(), (Expiration)ttl, default), Times.Once);
        _cache.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    private static string GetEventCacheKey(Guid eventId) => $"event:{eventId}";

    private static Event NewEvent(int totalSeats)
    {
        var startAt = DateTime.UtcNow.Date.AddDays(1);
        return new Event(Guid.NewGuid(), "Тестовое мероприятие", startAt, startAt.AddHours(1), totalSeats)
        {
            Description = "Описание мероприятия"
        };
    }

    private void SetupCacheWrite(string key, string json, TimeSpan ttl)
    {
        _cache.Setup(c => c.StringSetAsync(key, json, (Expiration)ttl, default))
            .ReturnsAsync(true);
    }

    private void VerifyCacheWrite(string key, string json, TimeSpan ttl)
    {
        _cache.Verify(c => c.StringSetAsync(key, json, (Expiration)ttl, default), Times.Once);
    }

    public void Dispose() => _provider.Dispose();
}

using EventPlatform.Events.Application;
using EventPlatform.Events.Application.DTO;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using EventPlatform.Events.Infrastructure.DbContexts;
using EventPlatform.Events.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventPlatform.Events.Tests;

public class EventServiceTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly AppDbContext _db;
    private readonly IEventService _eventService;

    public EventServiceTest()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton(Mock.Of<IEventMessageBrokerRepository>());
        services.AddApplication();

        _provider = services.BuildServiceProvider();

        _db = _provider.GetRequiredService<AppDbContext>();
        _eventService = _provider.GetRequiredService<IEventService>();
    }

    #region Success cases

    [Fact]
    public async Task CreateEvent_ShouldCallAddOnce()
    {
        // Arrange
        var addedEvents = 1;
        var initialCount = await _db.Events.CountAsync(TestContext.Current.CancellationToken);

        // Act
        var evt = await CreateTestEventAsync();

        // Assert
        var savedEvt = await _db.Events.SingleAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        savedEvt.Should().Be(evt);
        (await _db.Events.CountAsync(TestContext.Current.CancellationToken)).Should().Be(initialCount + addedEvents);
    }

    [Trait("Category", "Get")]
    [Fact]
    public async Task GetEventById_ReturnsOneCertainEvent()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var id = evt.Id;

        // Act
        var savedEvt = await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        savedEvt.Should().BeEquivalentTo(evt);
    }

    [Fact]
    public async Task UpdateEvent_ShouldCallUpdateOnce()
    {
        // Arrange
        var str = "Changed description";
        var evt = await CreateTestEventAsync();
        var id = evt.Id;
        var update = ToEventDto(evt);
        update.Description = str;

        // Act
        await _eventService.UpdateAsync(id, update, TestContext.Current.CancellationToken);

        // Assert
        evt = await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);
        evt.Description.Should().Be(str);
    }

    [Fact]
    public async Task UpdateEvent_DifferentIds_ThrowsArgumentException()
    {
        // Arrange
        var anotherId = Guid.NewGuid();
        var evt = await CreateTestEventAsync();

        // Act
        var act = async () => await _eventService.UpdateAsync(anotherId, ToEventDto(evt), TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(evt.Id));
    }

    [Fact]
    public async Task DeleteEvent_ShouldCallDeleteOnce()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var id = evt.Id;

        // Act
        await _eventService.DeleteAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        (await _db.Events.AnyAsync(e => e.Id == id, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Trait("Category", "Get")]
    [Fact]
    public async Task FilterEventByDate_ShouldReturnOneEventInBothProbes()
    {
        //-9   -1   1      9
        //|early|
        //|     |mid|
        //|     |late      | 

        // Arrange
        var earlyStartOffsetHours = -8;
        var earlyEndOffsetHours = -2;
        var lateEndOffsetHours = 8;
        var rangeStartOffsetHours = 1;
        var rangeEndOffsetHours = -1;
        var rangeStartOffsetMinutes = 15;
        var rangeEndOffsetMinutes = -15;
        var earlyEvt = await CreateTestEventAsync();
        earlyEvt.StartAt = earlyEvt.StartAt.AddHours(earlyStartOffsetHours);
        earlyEvt.EndAt = earlyEvt.EndAt.AddHours(earlyEndOffsetHours);
        await _eventService.UpdateAsync(earlyEvt.Id, ToEventDto(earlyEvt), TestContext.Current.CancellationToken);

        var midEvt = await CreateTestEventAsync();

        var lateEvt = await CreateTestEventAsync();
        lateEvt.EndAt = lateEvt.EndAt.AddHours(lateEndOffsetHours);
        await _eventService.UpdateAsync(lateEvt.Id, ToEventDto(lateEvt), TestContext.Current.CancellationToken);


        var earlySingleFrom = earlyEvt.StartAt.AddHours(rangeStartOffsetHours);
        var earlySingleTo = earlyEvt.EndAt.AddHours(rangeEndOffsetHours);

        var midLateFrom = midEvt.StartAt.AddMinutes(rangeStartOffsetMinutes);
        var midLateTo = midEvt.EndAt.AddMinutes(rangeEndOffsetMinutes);

        // Act
        var early = (await _eventService.GetAllAsync(TestContext.Current.CancellationToken, null, earlySingleFrom, earlySingleTo)).Data;
        var late = (await _eventService.GetAllAsync(TestContext.Current.CancellationToken, null, midLateFrom, midLateTo)).Data;

        // Assert
        early.Single().Should().BeEquivalentTo(earlyEvt);
        late.Should().BeEquivalentTo([midEvt, lateEvt], options => options.WithoutStrictOrdering());
    }

    [Trait("Category", "Get")]
    [Fact]
    public async Task EventPagination_ReturnsPaginatedResult()
    {
        // Arrange
        int pageNum = 1;
        int pageSize = 10;
        var emptyCount = 0;
        var evt = await CreateTestEventAsync();

        // Act
        var pagination = await _eventService.GetAllAsync(TestContext.Current.CancellationToken, null, null, null, pageNum, pageSize);

        // Assert
        pagination.Data.Count().Should().BeGreaterThan(emptyCount).And.BeLessThanOrEqualTo(pageSize);
        pagination.CurrentPage.Should().Be(pageNum);
        pagination.TotalItems.Should().BeGreaterThanOrEqualTo(pagination.PageItems);
    }

    //[Trait("Category", "Get")]
    //[Fact]
    //public void CombinedFilterEvent_ReturnsFilteredResultByTwoFields()
    //{
    //    // Arrange
    //    var title = "1";
    //    var to = testEvent1EndAt.AddHours(1);

    //    // Act
    //    var pagination = _eventService.GetAll(title, null, to);

    //    // Assert
    //    pagination.Data.Should().ContainSingle();
    //}

    [Trait("Category", "Get")]
    [Fact]
    public async Task CombinedFilterEventAnd_ReturnsEmptyResultByTwoFields()
    {
        // Arrange
        var title = Guid.NewGuid().ToString();
        var from = DateTime.MinValue;

        // Act
        var pagination = await _eventService.GetAllAsync(TestContext.Current.CancellationToken, title, from, null);

        // Assert
        pagination.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEvent_ShouldAvailableSeatsBeSameAsTotalSeats()
    {
        // Arrange
        int totalSeats = 4;

        // Act
        var evt = await CreateTestEventAsync(totalSeats);

        // Assert
        evt.AvailableSeats.Should().Be(totalSeats);
    }

    #endregion Success cases

    #region Failed cases

    [Trait("Category", "Get")]
    [Fact]
    public async Task GetNonExistedEventById_ThrowNotFoundException()
    {
        // Arrange
        var gid = Guid.NewGuid();

        // Act
        var act = async () => await _eventService.GetByIdAsync(gid, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateNonExistedEvent_ThrowsNotFoundException()
    {
        // Arrange
        var gid = Guid.NewGuid();
        var evt = await CreateTestEventAsync();

        // Act
        var act = async () => await _eventService.UpdateAsync(gid, ToEventDto(evt), TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(Event.Id));
    }

    [Fact]
    public async Task CreateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var invalidEndOffsetDays = -1;
        var evt = await CreateTestEventAsync();
        evt.EndAt = evt.StartAt.AddDays(invalidEndOffsetDays);

        // Act
        var act = async () => await _eventService.AddAsync(evt, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(evt.EndAt));
    }

    [Fact]
    public async Task UpdateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var invalidEndOffsetDays = -1;
        var evt = await CreateTestEventAsync();
        var gid = evt.Id;
        evt.EndAt = evt.StartAt.AddDays(invalidEndOffsetDays);

        // Act
        var act = async () => await _eventService.UpdateAsync(gid, ToEventDto(evt), TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(evt.EndAt));
    }

    #endregion Failed cases

    #region Edge cases

    [Fact]
    public async Task CreateEventWithMinMaxDate_Success()
    {
        // Arrange
        var evtMin = await CreateTestEventAsync();
        evtMin.StartAt = DateTime.MinValue;

        var evtMax = await CreateTestEventAsync();
        evtMax.EndAt = DateTime.MaxValue;

        // Act
        var actMin = async () => await _eventService.UpdateAsync(evtMin.Id, ToEventDto(evtMin), TestContext.Current.CancellationToken);
        var actMax = async () => await _eventService.UpdateAsync(evtMax.Id, ToEventDto(evtMax), TestContext.Current.CancellationToken);

        // Assert
        await actMin.Should().NotThrowAsync();
        await actMax.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetEventByEmptyId_ThrowAgrumentException()
    {
        // Arrange
        var id = Guid.Empty;

        // Act
        var act = async () => await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(id));
    }

    [Fact]
    public async Task EventPaginationWithNegativePageNumber_ThrowsArgumentException()
    {
        // Arrange
        int pageNum = -1;
        int pageSize = -10;
        var validPageNum = 1;

        // Act
        var paginationNegativePageNum = async () => await _eventService.GetAllAsync(TestContext.Current.CancellationToken, null, null, null, pageNum);
        var paginationNegativePageSize = async () => await _eventService.GetAllAsync(TestContext.Current.CancellationToken, null, null, null, validPageNum, pageSize);

        // Assert
        await paginationNegativePageNum.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("page");
        await paginationNegativePageSize.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("pageSize");
    }

    #endregion Edge cases

    async Task<Event> CreateTestEventAsync(int totalSeats = 0)
    {
        const int defaultSeats = 5;
        const int startOffsetHours = 1;
        const int endOffsetHours = 3;
        return await _eventService.CreateEventAsync(new EventRequest
        {
            Id = Guid.NewGuid(),
            Title = "Test event Title",
            Description = "Test event Description",
            StartAt = DateTime.UtcNow.AddHours(startOffsetHours),
            EndAt = DateTime.UtcNow.AddHours(endOffsetHours),
            TotalSeats = totalSeats > 0 ? totalSeats : defaultSeats
        }, TestContext.Current.CancellationToken);
    }

    public void Dispose() => _provider.Dispose();

    static EventRequest ToEventDto(Event evt, Guid? id = null) => new()
    {
        Id = id ?? evt.Id,
        Title = evt.Title,
        Description = evt.Description,
        StartAt = evt.StartAt,
        EndAt = evt.EndAt,
        TotalSeats = evt.TotalSeats
    };
}

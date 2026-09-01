using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using EventPlatform.Api.Repositories;
using EventPlatform.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventPlatform.Tests;

public class EventServiceTest
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
        services.AddScoped<IEventService, EventService>();

        _provider = services.BuildServiceProvider();

        _db = _provider.GetRequiredService<AppDbContext>();
        _eventService = _provider.GetRequiredService<IEventService>();
    }

    #region Success cases

    [Fact]
    public async Task CreateEvent_ShouldCallAddOnce()
    {
        // Arrange
        // Act
        var evt = await CreateTestEventAsync();
        var id = evt.Id;

        // Assert
        var savedEvt = await _db.Events.SingleAsync(e => e.Id == id, TestContext.Current.CancellationToken);
        savedEvt.Should().Be(evt);
    }

    [Trait("Category", "Get")]
    [Fact]
    public async Task GetAllEvents_ShouldReturnAllTestEvents()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var id = evt.Id;

        // Act
        var eventList = await _eventService.GetAllAsync(null, null, null);

        // Assert
        eventList.Data.Should().BeEquivalentTo(_db.Events);
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
        evt = await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);
        evt.Description = str;

        // Act
        await _eventService.UpdateAsync(id, evt, TestContext.Current.CancellationToken);

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
        var act = async () => await _eventService.UpdateAsync(anotherId, evt, TestContext.Current.CancellationToken);

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
        //await _eventService.DeleteAsync(evt.Id, TestContext.Current.CancellationToken); // Not appliable with InMemory provider
        _db.Events.Remove(evt);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        _db.Events.Any(e => e.Id == id).Should().BeFalse();
    }

    [Trait("Category", "Get")]
    [Fact]
    public async Task FilterEventByName_ShouldReturnOneEventWithMatchingName()
    {
        // Arrange
        var seed = Guid.NewGuid();
        var title = $"Test Event {seed}";
        var evt = await CreateTestEventAsync();
        evt.Title = title;
        await _eventService.UpdateAsync(evt.Id, evt, TestContext.Current.CancellationToken);

        // Act
        var list = (await _eventService.GetAllAsync(seed.ToString(), null, null)).Data;

        // Assert
        list.Should().ContainSingle(e => e.Title == title);
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
        var earlyEvt = await CreateTestEventAsync();
        earlyEvt.StartAt = earlyEvt.StartAt.AddHours(-8);
        earlyEvt.EndAt = earlyEvt.EndAt.AddHours(-2);
        await _eventService.UpdateAsync(earlyEvt.Id, earlyEvt, TestContext.Current.CancellationToken);

        var midEvt = await CreateTestEventAsync();

        var lateEvt = await CreateTestEventAsync();
        lateEvt.EndAt = lateEvt.EndAt.AddHours(8);
        await _eventService.UpdateAsync(lateEvt.Id, lateEvt, TestContext.Current.CancellationToken);


        var earlySingleFrom = earlyEvt.StartAt.AddHours(1);
        var earlySingleTo = earlyEvt.EndAt.AddHours(-1);

        var midLateFrom = midEvt.StartAt.AddMinutes(15);
        var midLateTo = midEvt.EndAt.AddMinutes(-15);

        // Act
        var early = (await _eventService.GetAllAsync(null, earlySingleFrom, earlySingleTo)).Data;
        var late = (await _eventService.GetAllAsync(null, midLateFrom, midLateTo)).Data;

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
        var evt = await CreateTestEventAsync();

        // Act
        var pagination = await _eventService.GetAllAsync(null, null, null, pageNum, pageSize);

        // Assert
        pagination.Data.Count().Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(pageSize);
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
        var pagination = await _eventService.GetAllAsync(title, from, null);

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
        var act = async () => await _eventService.GetByIdAsync(gid);

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
        var act = async () => await _eventService.UpdateAsync(gid, evt);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(Event.Id));
    }

    [Fact]
    public async Task CreateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var gid = Guid.NewGuid();
        var evt = await CreateTestEventAsync();
        evt.EndAt = evt.StartAt.AddDays(-1);

        // Act
        var act = async () => await _eventService.AddAsync(evt);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(evt.EndAt));
    }

    [Fact]
    public async Task UpdateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var evt = await CreateTestEventAsync();
        var gid = evt.Id;
        evt.EndAt = evt.StartAt.AddDays(-1);

        // Act
        var act = async () => await _eventService.UpdateAsync(gid, evt);

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
        var actMin = async () => await _eventService.UpdateAsync(evtMin.Id, evtMin, TestContext.Current.CancellationToken);
        var actMax = async () => await _eventService.UpdateAsync(evtMax.Id, evtMax, TestContext.Current.CancellationToken);

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
        var act = async () => await _eventService.GetByIdAsync(id);

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

        // Act
        var paginationNegativePageNum = async () => await _eventService.GetAllAsync(null, null, null, pageNum);
        var paginationNegativePageSize = async () => await _eventService.GetAllAsync(null, null, null, 1, pageSize);

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
        return await _eventService.CreateEventAsync(
                Guid.NewGuid(),
                "Test event Title",
                "Test event Description",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(3),
                totalSeats > 0 ? totalSeats : new Random().Next(3, 8)
            );
    }
}

using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using EventPlatform.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventPlatform.Tests;

public class EventServiceTest
{
    private readonly Mock<ILogger<EventService>> _logger;
    private readonly Mock<IEventStorage> _eventStorageMock;
    private readonly IEventService _eventService;
    private readonly IEnumerable<Event> _events;
    private readonly Guid testEvent1Gid;
    private readonly DateTime testEvent1StartAt;
    private readonly DateTime testEvent1EndAt;
    private readonly Guid testEvent2Gid;
    private readonly DateTime testEvent2StartAt;
    private readonly DateTime testEvent2EndAt;

    public EventServiceTest()
    {
        testEvent1Gid = Guid.Parse("B6780633-10B5-40DF-B129-B7A01D6F7EE6");
        testEvent1StartAt = new DateTime(2026, 04, 01, 10, 0, 0);
        testEvent1EndAt = new DateTime(2026, 04, 01, 12, 0, 0);

        testEvent2Gid = Guid.Parse("3F04F96B-FA2A-46DF-81E8-F44E006B8271");
        testEvent2StartAt = new DateTime(2026, 04, 02, 16, 0, 0);
        testEvent2EndAt = new DateTime(2026, 04, 02, 18, 0, 0);

        _events = [
            new Event
            {
                Id = testEvent1Gid,
                Title = "Test Event 1",
                Description = "This is a test event",
                StartAt = testEvent1StartAt,
                EndAt = testEvent1EndAt,
            },
            new Event
            {
                Id = testEvent2Gid,
                Title = "Test Event 2",
                Description = "This is a test event",
                StartAt = testEvent2StartAt,
                EndAt = testEvent2EndAt,
            },
        ];

        _logger = new Mock<ILogger<EventService>>();
        _eventStorageMock = new Mock<IEventStorage>();
        _eventStorageMock.Setup(storage => storage.GetAll()).Returns(() => _events);
        _eventStorageMock.Setup(storage => storage.Delete(It.IsAny<Guid>())).Callback<Guid>(id =>
        {
            if (!_events.Any(e => e.Id == id))
                throw new KeyNotFoundException();
        });
        _eventStorageMock.Setup(storage => storage.Update(It.IsAny<Guid>(), It.IsAny<Event>())).Callback<Guid, Event>((id, obj) =>
        {
            if (!_events.Any(e => e.Id == id))
                throw new KeyNotFoundException();
        });
        _eventStorageMock.Setup(storage => storage.GetById(It.IsAny<Guid>())).Returns<Guid>(id =>
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt == null)
                throw new KeyNotFoundException();
            return evt;
        });
        _eventService = new EventService(_eventStorageMock.Object, _logger.Object);
    }

    #region Success cases

    [Fact]
    public void CreateEvent_ShouldCallAddOnce()
    {
        // Arrange
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event to create",
            Description = "This is a test event",
            StartAt = DateTime.UtcNow.AddHours(-1),
            EndAt = DateTime.UtcNow.AddHours(1),
        };

        // Act
        _eventService.Add(evt);

        // Assert
        _eventStorageMock.Verify(storage => storage.Add(evt), Times.Once);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void GetAllEvents_ShouldReturnAllTestEvents()
    {
        // Arrange

        // Act
        var eventList = _eventService.GetAll(null, null, null);

        // Assert
        eventList.Data.Should().BeEquivalentTo(_events);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void GetEventById_ReturnsOneCertainEvent()
    {
        // Arrange
        var evtToReturn = _events.First();

        // Act
        var evt = _eventService.GetById(testEvent1Gid);

        // Assert
        evt.Should().BeEquivalentTo(evtToReturn);
    }

    [Fact]
    public void UpdateEvent_ShouldCallUpdateOnce()
    {
        // Arrange
        var evt = new Event
        {
            Id = testEvent1Gid,
            Title = "Test Event 1 Updated",
            Description = "This is a test event",
            StartAt = testEvent1StartAt,
            EndAt = testEvent1EndAt,
        };

        // Act
        _eventService.Update(testEvent1Gid, evt);

        // Assert
        _eventStorageMock.Verify(storage => storage.Update(testEvent1Gid, evt), Times.Once);
    }

    [Fact]
    public void UpdateEvent_DifferentIds_ThrowsArgumentException()
    {
        // Arrange
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event 1 Updated",
            Description = "This is a test event",
            StartAt = testEvent1StartAt,
            EndAt = testEvent1EndAt,
        };

        // Act
        var act = () => _eventService.Update(testEvent1Gid, evt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(evt.Id));
    }

    [Fact]
    public void DeleteEvent_ShouldCallDeleteOnce()
    {
        // Arrange

        // Act
        _eventService.Delete(testEvent1Gid);

        // Assert
        _eventStorageMock.Verify(storage => storage.Delete(testEvent1Gid), Times.Once);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void FilterEventByName_ShouldReturnOneEventWithMatchingName()
    {
        // Arrange
        var title = "Test Event 1";

        // Act
        var evt = _eventService.GetAll(title, null, null).Data.Single();

        // Assert
        evt.Id.Should().Be(testEvent1Gid);
        evt.Title.Should().Contain(title);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void FilterEventByDate_ShouldReturnOneEventInBothProbes()
    {
        // Arrange
        var from = testEvent2StartAt.AddHours(-1);
        var expectedFrom = _events.Last();
        var to = testEvent1EndAt.AddHours(1);
        var expectedTo = _events.First();

        // Act
        var fromEvent = _eventService.GetAll(null, from, null).Data;
        var toEvent = _eventService.GetAll(null, null, to).Data;

        // Assert
        fromEvent.Should().ContainSingle();
        fromEvent.Single().Should().BeEquivalentTo(expectedFrom);
        toEvent.Should().ContainSingle();
        toEvent.Single().Should().BeEquivalentTo(expectedTo);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void EventPagination_ReturnsPaginatedResult()
    {
        // Arrange
        int pageNum = 1;
        int pageSize = 10;

        // Act
        var pagination = _eventService.GetAll(null, null, null, pageNum, pageSize);

        // Assert
        pagination.Data.Count().Should().Be(_events.Count());
        Assert.Equal(_events.Count() % pageSize, pagination.PageItems);
        Assert.Equal(_events.Count(), pagination.TotalItems);
        pagination.CurrentPage.Should().Be(pageNum);
    }

    [Trait("Category", "Get")]
    [Fact]
    public void CombinedFilterEvent_ReturnsFilteredResultByTwoFields()
    {
        // Arrange
        var title = "1";
        var to = testEvent1EndAt.AddHours(1);

        // Act
        var pagination = _eventService.GetAll(title, null, to);

        // Assert
        pagination.Data.Should().ContainSingle();
    }

    [Trait("Category", "Get")]
    [Fact]
    public void CombinedFilterEventAnd_ReturnsEmptyResultByTwoFields()
    {
        // Arrange
        var title = "test";
        var from = testEvent2EndAt.AddHours(1);

        // Act
        var pagination = _eventService.GetAll(title, from, null);

        // Assert
        pagination.Data.Should().BeEmpty();
    }

    #endregion Success cases

    #region Failed cases

    [Trait("Category", "Get")]
    [Fact]
    public void GetNonExistedEventById_ThrowNotFoundException()
    {
        // Arrange
        var gid = Guid.NewGuid();

        // Act
        var act = () => _eventService.GetById(gid);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void UpdateNonExistedEvent_ThrowsNotFoundException()
    {
        // Arrange
        var gid = Guid.NewGuid();
        var evt = new Event
        {
            Id = gid,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now,
        };

        // Act
        var act = () => _eventService.Update(gid, evt);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void CreateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var gid = Guid.NewGuid();
        var evt = new Event
        {
            Id = gid,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.Now.AddHours(1),
            EndAt = DateTime.Now.AddHours(-1),
        };

        // Act
        var act = () => _eventService.Add(evt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(evt.EndAt));
    }

    [Fact]
    public void UpdateEventWithInvalidParams_ThrowsArgumentException()
    {
        // Arrange
        var evt = new Event
        {
            Id = testEvent1Gid,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.Now.AddHours(1),
            EndAt = DateTime.Now.AddHours(-1),
        };

        // Act
        var act = () => _eventService.Update(testEvent1Gid, evt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(evt.EndAt));
    }

    #endregion Failed cases

    #region Edge cases

    [Fact]
    public void CreateEventWithMinMaxDate_Success()
    {
        // Arrange
        var gid = Guid.NewGuid();
        var evtMin = new Event
        {
            Id = gid,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.MinValue,
        };

        var evtMax = new Event
        {
            Id = gid,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.MaxValue,
            EndAt = DateTime.MaxValue,
        };

        // Act
        var actMin = () => _eventService.Add(evtMin);
        var actMax = () => _eventService.Add(evtMax);

        // Assert
        actMin.Should().NotThrow();
        actMax.Should().NotThrow();
    }

    [Fact]
    public void GetEventByEmptyId_ThrowAgrumentException()
    {
        // Arrange
        var id = Guid.Empty;

        // Act
        var act = () => _eventService.GetById(id);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName(nameof(id));
    }

    [Fact]
    public void EventPaginationWithNegativePageNumber_ThrowsArgumentException()
    {
        // Arrange
        int pageNum = -1;
        int pageSize = -10;

        // Act
        var paginationNegativePageNum = () => _eventService.GetAll(null, null, null, pageNum);
        var paginationNegativePageSize = () => _eventService.GetAll(null, null, null, 1, pageSize);

        // Assert
        paginationNegativePageNum.Should()
            .Throw<ArgumentException>()
            .WithParameterName("page");
        paginationNegativePageSize.Should()
            .Throw<ArgumentException>()
            .WithParameterName("pageSize");
    }

    #endregion Edge cases
}

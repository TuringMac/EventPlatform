using EventPlatform.Events.Application;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventPlatform.Events.Tests;

public class EventServiceMessagingTests : IDisposable
{
    private readonly Mock<IEventRepository> _repository = new();
    private readonly Mock<IEventMessageBrokerRepository> _broker = new(MockBehavior.Strict);
    private readonly ServiceProvider _provider;
    private readonly IEventService _service;

    public EventServiceMessagingTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(_repository.Object);
        services.AddSingleton(_broker.Object);
        _provider = services.BuildServiceProvider();
        _service = _provider.GetRequiredService<IEventService>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeSeats_PersistsBeforePublishingSuccess(bool release)
    {
        // Arrange
        var totalSeats = 4;
        var reservedSeats = 1;
        var evt = NewEvent(totalSeats);
        if (release)
            evt.TryReserveSeats().Should().BeTrue();
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var observed = new List<string>();
        _repository.Setup(r => r.GetForUpdateAsync(evt.Id, token)).ReturnsAsync(evt);
        _repository.Setup(r => r.UpdateAsync(evt.Id, evt, token))
            .Callback(() => observed.Add("saved")).Returns(Task.CompletedTask);
        if (release)
            _broker.Setup(b => b.SeatsReleasedAsync(evt.Id, bookingId, token))
                .Callback(() => observed.Add("requested")).Returns(Task.CompletedTask);
        else
            _broker.Setup(b => b.SeatsReservedAsync(evt.Id, bookingId, token))
                .Callback(() => observed.Add("requested")).Returns(Task.CompletedTask);

        // Act
        var result = await ChangeSeatsAsync(release, evt.Id, bookingId, token);

        // Assert
        result.Should().BeTrue();
        evt.AvailableSeats.Should().Be(release ? totalSeats : totalSeats - reservedSeats);
        observed.Should().Equal("saved", "requested");
        _repository.Verify(r => r.UpdateAsync(evt.Id, evt, token), Times.Once);
        VerifySuccess(release, evt.Id, bookingId, token);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("ended")]
    [InlineData("full")]
    public async Task ReserveSeats_WhenUnavailable_PublishesFailureWithoutSaving(string reason)
    {
        // Arrange
        var totalSeats = 1;
        var evt = NewEvent(totalSeats, ended: reason == "ended");
        if (reason == "full")
            evt.TryReserveSeats().Should().BeTrue();
        var seatsBefore = evt.AvailableSeats;
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        _repository.Setup(r => r.GetForUpdateAsync(evt.Id, token)).ReturnsAsync(evt);
        _broker.Setup(b => b.SeatsReservationFailedAsync(evt.Id, bookingId, token)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReserveSeats(evt.Id, bookingId, token);

        // Assert
        result.Should().BeFalse();
        evt.AvailableSeats.Should().Be(seatsBefore);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _broker.Verify(b => b.SeatsReservationFailedAsync(evt.Id, bookingId, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseSeats_WhenEventEnded_PublishesFailureWithoutSaving()
    {
        // Arrange
        var totalSeats = 4;
        var evt = NewEvent(totalSeats, ended: true);
        evt.TryReserveSeats().Should().BeTrue();
        var seatsBefore = evt.AvailableSeats;
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        _repository.Setup(r => r.GetForUpdateAsync(evt.Id, token)).ReturnsAsync(evt);
        _broker.Setup(b => b.SeatsReleaseFailedAsync(evt.Id, bookingId, token)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReleaseSeats(evt.Id, bookingId, token);

        // Assert
        result.Should().BeFalse();
        evt.AvailableSeats.Should().Be(seatsBefore);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _broker.Verify(b => b.SeatsReleaseFailedAsync(evt.Id, bookingId, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeSeats_WhenEventMissing_DoesNotPublish(bool release)
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;

        // Act
        var act = () => ChangeSeatsAsync(release, eventId, bookingId, token);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeSeats_WhenSavingFails_DoesNotPublish(bool release)
    {
        // Arrange
        var totalSeats = 4;
        var evt = NewEvent(totalSeats);
        if (release)
            evt.TryReserveSeats().Should().BeTrue();
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var error = new InvalidOperationException("Ошибка сохранения мероприятия");
        _repository.Setup(r => r.GetForUpdateAsync(evt.Id, token)).ReturnsAsync(evt);
        _repository.Setup(r => r.UpdateAsync(evt.Id, evt, token)).ThrowsAsync(error);

        // Act
        var act = () => ChangeSeatsAsync(release, evt.Id, bookingId, token);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(error);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeSeats_WhenPublishingFails_PropagatesException(bool release)
    {
        // Arrange
        var totalSeats = 4;
        var evt = NewEvent(totalSeats);
        if (release)
            evt.TryReserveSeats().Should().BeTrue();
        var bookingId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var error = new InvalidOperationException("Ошибка отправки сообщения");
        _repository.Setup(r => r.GetForUpdateAsync(evt.Id, token)).ReturnsAsync(evt);
        if (release)
            _broker.Setup(b => b.SeatsReleasedAsync(evt.Id, bookingId, token)).ThrowsAsync(error);
        else
            _broker.Setup(b => b.SeatsReservedAsync(evt.Id, bookingId, token)).ThrowsAsync(error);

        // Act
        var act = () => ChangeSeatsAsync(release, evt.Id, bookingId, token);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(error);
        _repository.Verify(r => r.UpdateAsync(evt.Id, evt, token), Times.Once);
        VerifySuccess(release, evt.Id, bookingId, token);
        _broker.VerifyNoOtherCalls();
    }

    private Task<bool> ChangeSeatsAsync(bool release, Guid eventId, Guid bookingId, CancellationToken token) => release
        ? _service.ReleaseSeats(eventId, bookingId, token)
        : _service.ReserveSeats(eventId, bookingId, token);

    private void VerifySuccess(bool release, Guid eventId, Guid bookingId, CancellationToken token)
    {
        if (release)
            _broker.Verify(b => b.SeatsReleasedAsync(eventId, bookingId, token), Times.Once);
        else
            _broker.Verify(b => b.SeatsReservedAsync(eventId, bookingId, token), Times.Once);
    }

    private static Event NewEvent(int seats, bool ended = false)
    {
        const int pastStartHours = -2;
        const int pastEndHours = -1;
        const int futureEndHours = 1;
        var now = DateTime.UtcNow;
        return new Event(Guid.NewGuid(), "Test event", now.AddHours(pastStartHours),
            now.AddHours(ended ? pastEndHours : futureEndHours), seats);
    }

    public void Dispose() => _provider.Dispose();
}

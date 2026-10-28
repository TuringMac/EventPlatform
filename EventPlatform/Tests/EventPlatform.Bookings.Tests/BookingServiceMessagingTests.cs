using EventPlatform.Bookings.Application;
using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Options;
using EventPlatform.Bookings.Domain.Exceptions;
using EventPlatform.Bookings.Domain.Model;
using EventPlatform.Common.Application.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace EventPlatform.Bookings.Tests;

public class BookingServiceMessagingTests : IDisposable
{
    private readonly Mock<IBookingRepository> _repository = new();
    private readonly Mock<IBookingMessageBrokerRepository> _broker = new(MockBehavior.Strict);
    private readonly ServiceProvider _provider;
    private readonly IBookingService _service;

    public BookingServiceMessagingTests()
    {
        var bookingLimit = 3;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddOptions<BookingOptions>().Configure(options => options.PerUserLimit = bookingLimit);
        services.AddSingleton(_repository.Object);
        services.AddSingleton(_broker.Object);
        _provider = services.BuildServiceProvider();
        _service = _provider.GetRequiredService<IBookingService>();
    }

    [Fact]
    public async Task CreateBookingAsync_SavesPendingBookingBeforePublishingRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var observed = new List<string>();
        Booking? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Booking>(), token))
            .Callback<Booking, CancellationToken>((booking, _) =>
            {
                saved = booking;
                observed.Add("saved");
            }).Returns(Task.CompletedTask);
        _broker.Setup(b => b.CreateRequestedAsync(It.IsAny<Guid>(), eventId, userId, token))
            .Callback(() => observed.Add("requested")).Returns(Task.CompletedTask);

        // Act
        var booking = await _service.CreateBookingAsync(eventId, userId, token);

        // Assert
        saved.Should().BeSameAs(booking);
        booking.Status.Should().Be(BookingStatusEnum.Pending);
        booking.ProcessedAt.Should().BeNull();
        observed.Should().Equal("saved", "requested");
        _repository.Verify(r => r.AddAsync(booking, token), Times.Once);
        _broker.Verify(b => b.CreateRequestedAsync(booking.Id, eventId, userId, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSavingFails_DoesNotPublish()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        var error = new InvalidOperationException("Ошибка сохранения бронирования");
        _repository.Setup(r => r.AddAsync(It.IsAny<Booking>(), token)).ThrowsAsync(error);

        // Act
        var act = () => _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), token);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(error);
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateBookingAsync_WhenLimitReached_DoesNotPublish()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var limit = _provider.GetRequiredService<IOptions<BookingOptions>>().Value.PerUserLimit;
        _repository.Setup(r => r.CountUserBookings(userId, token)).ReturnsAsync(limit);

        // Act
        var act = () => _service.CreateBookingAsync(Guid.NewGuid(), userId, token);

        // Assert
        await act.Should().ThrowAsync<BookingLimitReachedException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateBookingAsync_WhenIdentifierEmpty_DoesNotPublish(bool emptyEventId)
    {
        // Arrange
        var eventId = emptyEventId ? Guid.Empty : Guid.NewGuid();
        var userId = emptyEventId ? Guid.NewGuid() : Guid.Empty;
        var token = TestContext.Current.CancellationToken;

        // Act
        var act = () => _service.CreateBookingAsync(eventId, userId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repository.VerifyNoOtherCalls();
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelBookingRequestByIdAsync_WhenAuthorized_PublishesWithoutChangingStatus(bool isAdmin)
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var callerId = isAdmin ? Guid.NewGuid() : booking.UserId;
        var token = TestContext.Current.CancellationToken;
        _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);
        _broker.Setup(b => b.CancelRequestedAsync(booking.Id, booking.EventId, token)).Returns(Task.CompletedTask);

        // Act
        await _service.CancelBookingRequestByIdAsync(booking.Id, callerId, isAdmin, token);

        // Assert
        booking.Status.Should().Be(BookingStatusEnum.Pending);
        booking.ProcessedAt.Should().BeNull();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _broker.Verify(b => b.CancelRequestedAsync(booking.Id, booking.EventId, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelBookingRequestByIdAsync_WhenForbidden_DoesNotPublish()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var token = TestContext.Current.CancellationToken;
        var isAdmin = false;
        _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);

        // Act
        var act = () => _service.CancelBookingRequestByIdAsync(booking.Id, Guid.NewGuid(), isAdmin, token);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelBookingRequestByIdAsync_WhenMissing_DoesNotPublish()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var isAdmin = false;

        // Act
        var act = () => _service.CancelBookingRequestByIdAsync(bookingId, userId, isAdmin, token);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _broker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelBookingRequestAsync_PublishesForBookingFoundByUserAndEvent()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var token = TestContext.Current.CancellationToken;
        var isAdmin = false;
        _repository.Setup(r => r.GetBookingIdByEventAndUserAsync(booking.EventId, booking.UserId, token)).ReturnsAsync(booking.Id);
        _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);
        _broker.Setup(b => b.CancelRequestedAsync(booking.Id, booking.EventId, token)).Returns(Task.CompletedTask);

        // Act
        await _service.CancelBookingRequestAsync(booking.EventId, booking.UserId, isAdmin, token);

        // Assert
        _broker.Verify(b => b.CancelRequestedAsync(booking.Id, booking.EventId, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Request_WhenPublishingFails_PropagatesException(bool cancel)
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var token = TestContext.Current.CancellationToken;
        var error = new InvalidOperationException("Ошибка отправки сообщения");
        var isAdmin = false;
        if (cancel)
        {
            _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);
            _broker.Setup(b => b.CancelRequestedAsync(booking.Id, booking.EventId, token)).ThrowsAsync(error);
        }
        else
            _broker.Setup(b => b.CreateRequestedAsync(It.IsAny<Guid>(), booking.EventId, booking.UserId, token)).ThrowsAsync(error);

        // Act
        Func<Task> act = cancel
            ? () => _service.CancelBookingRequestByIdAsync(booking.Id, booking.UserId, isAdmin, token)
            : async () => { await _service.CreateBookingAsync(booking.EventId, booking.UserId, token); };

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(error);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed)]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Cancelled)]
    public async Task HandleResult_UpdatesStatusAndTimestampWithoutPublishing(BookingStatusEnum status)
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var token = TestContext.Current.CancellationToken;
        var processedAt = DateTime.UtcNow;
        _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);

        // Act
        await HandleResultAsync(status, booking.Id, processedAt, token);

        // Assert
        booking.Status.Should().Be(status);
        booking.ProcessedAt.Should().Be(processedAt);
        _repository.Verify(r => r.UpdateAsync(booking, token), Times.Once);
        _broker.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed)]
    [InlineData(BookingStatusEnum.Rejected)]
    public async Task HandleResult_WhenRepeated_DoesNotSaveOrRequestAgain(BookingStatusEnum status)
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var token = TestContext.Current.CancellationToken;
        var processedAt = DateTime.UtcNow;
        _repository.Setup(r => r.GetByIdAsync(booking.Id, token)).ReturnsAsync(booking);
        await HandleResultAsync(status, booking.Id, processedAt, token);
        _repository.Invocations.Clear();

        // Act
        await HandleResultAsync(status, booking.Id, DateTime.UtcNow, token);

        // Assert
        booking.Status.Should().Be(status);
        booking.ProcessedAt.Should().Be(processedAt);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _broker.VerifyNoOtherCalls();
    }

    private Task HandleResultAsync(BookingStatusEnum status, Guid bookingId, DateTime processedAt, CancellationToken token) => status switch
    {
        BookingStatusEnum.Confirmed => _service.ConfirmBookingAsync(bookingId, processedAt, token),
        BookingStatusEnum.Rejected => _service.RejectBookingAsync(bookingId, processedAt, token),
        BookingStatusEnum.Cancelled => _service.CancelBookingAsync(bookingId, processedAt, token),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public void Dispose() => _provider.Dispose();
}

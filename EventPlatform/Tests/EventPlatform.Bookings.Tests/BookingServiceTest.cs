using EventPlatform.Bookings.Application;
using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Options;
using EventPlatform.Bookings.Domain.Exceptions;
using EventPlatform.Bookings.Domain.Model;
using EventPlatform.Bookings.Infrastructure.DbContexts;
using EventPlatform.Bookings.Infrastructure.Repositories;
using EventPlatform.Common.Application.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventPlatform.Bookings.Tests;

public class BookingServiceTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IBookingService _bookingService;

    public BookingServiceTest()
    {
        const int bookingLimit = 3;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddOptions<BookingOptions>().Configure(options => options.PerUserLimit = bookingLimit);
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton(Mock.Of<IBookingMessageBrokerRepository>());
        services.AddApplication();
        _provider = services.BuildServiceProvider();
        _bookingService = _provider.GetRequiredService<IBookingService>();
    }

    [Fact]
    public async Task CreateBookingForExistedEvent_ShouldBePendingAndCallAddOnce()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var booking = await CreateBookingAsync(eventId);

        // Assert
        booking.Status.Should().Be(BookingStatusEnum.Pending);
    }

    [Fact]
    public async Task CreateSomeBookingsForOneEvent_ShouldBeDifferentIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var first = await CreateBookingAsync(eventId);
        var second = await CreateBookingAsync(eventId);

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking()
    {
        // Arrange
        var booking = await CreateBookingAsync(Guid.NewGuid());

        // Act
        var loaded = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().Be(booking);
    }

    [Fact]
    public async Task UniqueConcurrencyTest_UniqueIds()
    {
        // Arrange
        var requestAmount = 10;
        var eventId = Guid.NewGuid();
        var firstIndex = 0;

        // Act
        var tasks = Enumerable.Range(firstIndex, requestAmount).Select(_ => CreateBookingAsync(eventId));
        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetBookingByNotExistingId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var act = () => _bookingService.GetBookingByIdAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetBookingById_WhenOwner_ReturnsBooking()
    {
        // Arrange
        var booking = await CreateBookingAsync(Guid.NewGuid());

        // Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id, booking.UserId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(booking);
    }

    [Fact]
    public async Task GetBookingById_WhenDifferentUser_ThrowsForbiddenException()
    {
        // Arrange
        var booking = await CreateBookingAsync(Guid.NewGuid());

        // Act
        var act = () => _bookingService.GetBookingByIdAsync(booking.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetBookingById_WhenNotFoundForUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var act = () => _bookingService.GetBookingByIdAsync(bookingId, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CancelBookingById_WhenBookingDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var isAdmin = false;

        // Act
        var act = () => _bookingService.CancelBookingRequestByIdAsync(bookingId, userId, isAdmin, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetBookingsByUserIdAsync_ReturnsOnlyUsersBookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var own = await CreateBookingAsync(eventId);
        await CreateBookingAsync(eventId);

        // Act
        var bookings = await _bookingService.GetBookingsByUserIdAsync(own.UserId, TestContext.Current.CancellationToken);

        // Assert
        bookings.Should().ContainSingle().Which.Id.Should().Be(own.Id);
    }

    [Fact]
    public async Task CancelBooking_WhenAlreadyCancelled_ThrowsWithoutChangingStatus()
    {
        // Arrange
        var booking = await CreateBookingAsync(Guid.NewGuid());
        await _bookingService.CancelBookingAsync(booking.Id, DateTime.UtcNow, TestContext.Current.CancellationToken);
        var processedAt = booking.ProcessedAt;

        // Act
        var act = () => _bookingService.CancelBookingAsync(booking.Id, DateTime.UtcNow, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<BookingAlreadyCancelledException>();
        booking.Status.Should().Be(BookingStatusEnum.Cancelled);
        booking.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public async Task CancelBooking_WhenRejected_ThrowsWithoutChangingStatus()
    {
        // Arrange
        var booking = await CreateBookingAsync(Guid.NewGuid());
        await _bookingService.RejectBookingAsync(booking.Id, DateTime.UtcNow, TestContext.Current.CancellationToken);
        var processedAt = booking.ProcessedAt;

        // Act
        var act = () => _bookingService.CancelBookingAsync(booking.Id, DateTime.UtcNow, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<BookingAlreadyCancelledException>();
        booking.Status.Should().Be(BookingStatusEnum.Rejected);
        booking.ProcessedAt.Should().Be(processedAt);
    }

    private Task<Booking> CreateBookingAsync(Guid eventId) =>
        _bookingService.CreateBookingAsync(eventId, Guid.NewGuid(), TestContext.Current.CancellationToken);

    public void Dispose() => _provider.Dispose();
}

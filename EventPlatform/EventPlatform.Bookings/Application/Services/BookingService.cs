using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Options;
using EventPlatform.Bookings.Domain.Exceptions;
using EventPlatform.Bookings.Domain.Model;
using EventPlatform.Common.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace EventPlatform.Bookings.Application.Services;

internal class BookingService(
    IBookingRepository _bookingRepository,
    IBookingMessageBrokerRepository _messageBrokerRepository,
    IOptions<BookingOptions> _bookingOptions,
    ILogger<BookingService> _logger) : IBookingService
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(nameof(eventId));
        if (userId == Guid.Empty)
            throw new ArgumentException(nameof(userId));

        var limit = _bookingOptions.Value.PerUserLimit;

        if (await _bookingRepository.CountUserBookings(userId, cancellationToken) >= limit)
            throw new BookingLimitReachedException($"Достигнут лимит {limit} Броней для Пользователя: {userId}");

        var booking = new Booking(eventId, userId);
        await _bookingRepository.AddAsync(booking, cancellationToken);
        _logger.LogInformation("Бронь {bookingId} добавлена в БД", booking.Id);
        await _messageBrokerRepository.CreateRequestedAsync(booking.Id, eventId, userId, cancellationToken);
        return booking;
    }

    public async Task CancelBookingRequestAsync(Guid eventId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(nameof(eventId));
        if (userId == Guid.Empty)
            throw new ArgumentException(nameof(userId));

        var bookingId = await _bookingRepository.GetBookingIdByEventAndUserAsync(eventId, userId, cancellationToken);
        if (bookingId == Guid.Empty)
            throw new KeyNotFoundException($"Бронирование для события {eventId} и пользователя {userId} не найдено");

        await CancelBookingRequestByIdAsync(bookingId, userId, isAdmin, cancellationToken);
    }

    public async Task CancelBookingRequestByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException(nameof(bookingId));
        if (userId == Guid.Empty)
            throw new ArgumentException(nameof(userId));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронирование {bookingId} не найдено");
        if (booking.UserId != userId && !isAdmin)
            throw new ForbiddenException($"Нет прав на отмену бронирования {bookingId}");

        await _messageBrokerRepository.CancelRequestedAsync(booking.Id, booking.EventId, cancellationToken);
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId));

        return await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронь {bookingId} не найдена");
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId), "Идентификатор брони не может быть пустым");
        if (userId == Guid.Empty)
            throw new ArgumentNullException(nameof(userId), "Идентификатор пользователя не может быть пустым");

        var booking = await GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking.UserId != userId)
            throw new ForbiddenException($"Пользователь {userId} не имеет прав для доступа к брони {bookingId}");
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentNullException(nameof(userId), "Идентификатор пользователя не может быть пустым");

        return await _bookingRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken, int batch = 50)
    {
        return await _bookingRepository.GetPendingIdsAsync(batch, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _bookingRepository.GetByEventIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронирования для события {eventId} не найдены");
    }

    public async Task ConfirmBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found");
        if (booking.Status == BookingStatusEnum.Confirmed) // Идемпотентность
            return;
        booking.Confirm(processedAt);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
    }

    public async Task RejectBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found");
        if (booking.Status == BookingStatusEnum.Rejected) // Идемпотентность
            return;
        booking.Reject(processedAt);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
    }

    public async Task CancelBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken cancellationToken)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException(nameof(bookingId));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Бронирование {bookingId} не найдено");

        booking.Cancel(processedAt);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        _logger.LogInformation("Бронь {bookingId} отменена", booking.Id);
    }
}

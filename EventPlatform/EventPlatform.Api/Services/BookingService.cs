using EventPlatform.Api.Exceptions;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingService(IBookingRepository _bookingRepository, IEventRepository _eventRepository, ILogger<BookingService> _logger) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);
    private static readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(nameof(eventId));

        await _bookingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (evt is null)
                throw new KeyNotFoundException($"Event {eventId} not found");
            if (evt.EndAt < DateTime.UtcNow)
                throw new InvalidOperationException();
            if (!evt.TryReserveSeats())
            {
                _logger.LogInformation("Booking запрос отклонен, нет доступных мест");
                throw new NoAvailableSeatsException("Нет доступных мест для события");
            }

            try
            {
                var booking = new Booking(eventId);
                await _bookingRepository.AddAsync(booking, cancellationToken);
                _logger.LogInformation("Бронь {bookingId} добавлена в БД", booking.Id);
                _logger.LogInformation("Событие {eventId} обновлено в БД", evt.Id);
                return booking;
            }
            catch (Exception ex)
            {
                evt.ReleaseSeats();
                _logger.LogError(ex, "Ошибка при сохранении Booking запроса в БД, места возвращены");
                throw;
            }
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId));
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");
        return booking;
    }

    public async Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken = default, int batch = 50)
    {
        return await _bookingRepository.GetPendingIdsAsync(batch, cancellationToken);
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Обработка брони {bookingId}", bookingId);
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null)
                    throw new KeyNotFoundException($"Booking {bookingId} not found");

                var evt = await _eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                try
                {
                    if (evt is null)
                    {
                        booking.Reject();
                        _logger.LogWarning("Событие {eventId} не существует. Бронь {bookingId} отменяется.", booking.EventId, booking.Id);
                    }
                    else if (evt.EndAt >= DateTime.UtcNow)
                    {
                        booking.Confirm();
                        _logger.LogInformation("Booking запрос {bookingId} подтвержден", booking.Id);
                    }
                    else
                    {
                        booking.Reject();
                        evt.ReleaseSeats();
                        _logger.LogInformation("Бронь {bookingId} от менена, мероприятие закончилось", booking.Id);
                    }
                    _logger.LogInformation("Бронь {bookingId} обновлена в БД", booking.Id);
                }
                catch (Exception ex)
                {
                    booking.Reject();
                    evt?.ReleaseSeats();
                    _logger.LogWarning(ex, "Ошибка при обработке брони {bookingId}. Бронь отклонена и обновлена в БД", booking.Id);
                }

                await _bookingRepository.UpdateAsync(booking, stoppingToken);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Обработка брони {bookingId} была отменена", bookingId);
            throw;
        }
    }
}

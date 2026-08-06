using EventPlatform.Api.Exceptions;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingService(IBookingStorage _bookingStorage, IEventService _eventService, ILogger<BookingService> _logger) : IBookingService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly Lock _bookingLock = new();
    private readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(nameof(eventId));
        lock (_bookingLock)
        {
            var evt = _eventService.GetById(eventId); // Выбросит исключение, если событие не найдено
            if (evt.TryReserveSeats())
            {
                try
                {
                    var booking = new Booking
                    {
                        EventId = eventId
                    };
                    _eventService.Update(eventId, evt);
                    _logger.LogInformation("Событие {eventId} обновлено в БД", evt.Id);
                    _bookingStorage.Add(booking);
                    _logger.LogInformation("Бронь {bookingId} добавлена в БД", booking.Id);
                    return booking;
                }
                catch
                {
                    evt.ReleaseSeats();
                    _logger.LogError("Ошибка при сохранении Booking запроса в БД, места возвращены");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Booking запрос отклонен, нет доступных мест");
                throw new NoAvailableSeatsException("Нет доступных мест для события");
            }
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentNullException(nameof(bookingId));
        return _bookingStorage.GetById(bookingId);
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default, int batch = 50)
    {
        return _bookingStorage.GetAll().Where(b => b.Status == BookingStatusEnum.Pending).Take(batch);
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Обработка брони {bookingId}", booking.Id);
        try
        {
            await Task.Delay(ProcessingDelay);

            await _processingSemaphore.WaitAsync(stoppingToken);
            var evt = _eventService.GetById(booking.EventId);
            try
            {
                if (evt != null)
                {
                    booking.Confirm();
                    _logger.LogInformation("Booking запрос {bookingId} подтвержден", booking.Id);
                }
                else
                {
                    booking.Reject();
                    _logger.LogWarning("Событие {eventId} не существует. Бронь {bookingId} отменяется.", booking.EventId, booking.Id);
                }
                _bookingStorage.Update(booking.Id, booking);
                _logger.LogInformation("Бронь {bookingId} обновлена в БД", booking.Id);
            }
            catch (Exception ex)
            {
                booking.Reject();
                _bookingStorage.Update(booking.Id, booking);
                evt.ReleaseSeats();
                _eventService.Update(evt.Id, evt);
                _logger.LogWarning(ex, "Ошибка при обработке брони {bookingId}. Бронь отклонена и обновлена в БД", booking.Id);
            }
        }
        catch(OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Обработка брони {bookingId} была отменена", booking.Id);
            throw;
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}

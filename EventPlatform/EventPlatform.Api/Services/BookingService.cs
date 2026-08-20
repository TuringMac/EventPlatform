using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Exceptions;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Api.Services;

public class BookingService(AppDbContext _context, IEventService _eventService, ILogger<BookingService> _logger) : IBookingService
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
            var evt = await _eventService.GetById(eventId); // Выбросит исключение, если событие не найдено
            if (evt.TryReserveSeats())
            {
                try
                {
                    var booking = new Booking(eventId);
                    evt.Bookings.Add(booking);
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
                finally
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("Booking запрос отклонен, нет доступных мест");
                throw new NoAvailableSeatsException("Нет доступных мест для события");
            }
        }
        catch (Exception ex)
        {
            throw;
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
        return await _context.Bookings.SingleAsync(b => b.Id == bookingId);
    }

    public async Task<IEnumerable<Guid>> GetPendingBookingsAsync(CancellationToken cancellationToken = default, int batch = 50)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatusEnum.Pending)
            .OrderBy(b => b.CreatedAt)
            .Take(batch)
            .Select(b => b.Id)
            .ToListAsync();
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Обработка брони {bookingId}", bookingId);
        try
        {
            await Task.Delay(ProcessingDelay);

            await _processingSemaphore.WaitAsync(stoppingToken);
            var booking = await _context.Bookings.SingleAsync(b => b.Id == bookingId);
            try
            {
                var evt = await _eventService.GetById(booking.EventId);
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
                    _logger.LogInformation("Бронь {bookingId} обновлена в БД", booking.Id);
                }
                catch (Exception ex)
                {
                    booking.Reject();
                    evt.ReleaseSeats();
                    _logger.LogWarning(ex, "Ошибка при обработке брони {bookingId}. Бронь отклонена и обновлена в БД", booking.Id);
                }
                finally
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (KeyNotFoundException)
            {
                booking.Reject();
                await _context.SaveChangesAsync();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Обработка брони {bookingId} была отменена", bookingId);
            throw;
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}

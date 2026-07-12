using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingBackgroundService(IBookingStorage _bookingStorage, ILogger<BookingBackgroundService> _logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(nameof(BookingBackgroundService) + " запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling {bookStatus} bookings", BookingStatusEnum.Pending);
                if (_bookingStorage.GetAll().Any(b => b.Status == BookingStatusEnum.Pending))
                {
                    _logger.LogInformation("Retrieving {bookStatus} booking", BookingStatusEnum.Pending);
                    var book = _bookingStorage.GetAll().First(b => b.Status == BookingStatusEnum.Pending);

                    _logger.LogInformation("Processing {bookId} booking", book.Id);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    _bookingStorage.Update(book.Id, new Booking
                    {
                        EventId = book.EventId,
                        Status = BookingStatusEnum.Confirmed,
                        ProcessedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Booking {bookId} confirmed successfully", book.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке бронирования");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        _logger.LogInformation(nameof(BookingBackgroundService) + " остановлен");
    }
}

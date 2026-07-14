using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingBackgroundService(ILogger<BookingBackgroundService> _logger, IServiceScopeFactory _scopeFactory) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(nameof(BookingBackgroundService) + " запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling {bookStatus} bookings", BookingStatusEnum.Pending);
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                if ((await bookingService.GetPendingBookingsAsync(stoppingToken)).Any())
                {
                    _logger.LogInformation("Retrieving {bookStatus} booking", BookingStatusEnum.Pending);
                    var book = (await bookingService.GetPendingBookingsAsync(stoppingToken)).First();

                    _logger.LogInformation("Processing {bookId} booking", book.Id);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    await bookingService.ConfirmAsync(book, stoppingToken);
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

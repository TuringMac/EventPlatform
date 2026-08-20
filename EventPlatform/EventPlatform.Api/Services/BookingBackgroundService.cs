using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Services;

public class BookingBackgroundService(ILogger<BookingBackgroundService> _logger, IServiceScopeFactory _scopeFactory) : BackgroundService
{
    private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(nameof(BookingBackgroundService) + " запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling {bookStatus} bookings", BookingStatusEnum.Pending);
                List<Guid> pendingBookings;
                using (var pendingBookingsScope = _scopeFactory.CreateScope())
                {
                    var bookingService = pendingBookingsScope.ServiceProvider.GetRequiredService<IBookingService>();

                    pendingBookings = (await bookingService.GetPendingBookingsAsync(stoppingToken)).ToList();
                }
                var tasks = pendingBookings.Select(async bookingId =>
                {
                    await using var processBookingScope = _scopeFactory.CreateAsyncScope();
                    var processBookingService = processBookingScope.ServiceProvider.GetRequiredService<IBookingService>();
                    await processBookingService.ProcessBookingAsync(bookingId, stoppingToken);
                });

                var pollingTask = Task.Delay(PollingInterval, stoppingToken);
                await Task.WhenAll(tasks.Append(pollingTask));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке бронирования");
            }
        }

        _logger.LogInformation(nameof(BookingBackgroundService) + " остановлен");
    }
}

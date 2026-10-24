using Confluent.Kafka;
using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Options;
using EventPlatform.Contracts;
using EventPlatform.Contracts.Events;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventPlatform.Bookings.Infrastructure.Services;

public class BookingBackgroundService(IOptions<BookingMessageBrokerOptions> options, ILogger<BookingBackgroundService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () => await Consume(stoppingToken), stoppingToken);
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            // Все консьюмеры с одинаковым GroupId делят партиции топика между собой
            GroupId = options.Value.ConsumerGroup,
            // Earliest — при первом запуске читать с начала топика
            // (если у группы ещё нет сохранённого офсета)
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // false — управляем коммитом офсета вручную (at-least-once)
            EnableAutoCommit = false,
            // false — управляем позицией смещения вручную
            EnableAutoOffsetStore = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        // Kafka назначит консьюмеру партиции в рамках перебалансировки группы
        consumer.Subscribe(Topics.Events);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Ожидание сообщений из топика '{Topic}'...", Topics.Events);
                    // Блокирующий вызов: ждём следующего сообщения от брокера (pull-модель)
                    var consumeResult = consumer.Consume(stoppingToken);

                    var headers = consumeResult.Message.Headers;
                    if (headers is null ||
                        !headers.TryGetLastBytes(options.Value.MessageTypeHeaderName, out var eventTypeBytes) ||
                        eventTypeBytes is null)
                    {
                        throw new InvalidOperationException($"В сообщении отсутствует заголовок {options.Value.MessageTypeHeaderName}.");
                    }

                    // Определяем тип события по заголовку и обрабатываем соответствующим образом
                    var eventType = System.Text.Encoding.UTF8.GetString(eventTypeBytes);
                    // Места успешно зарезервированы, подтверждаем бронь
                    if (eventType == nameof(EventSeatsReservedMessage))
                    {
                        var evt = JsonSerializer.Deserialize<EventSeatsReservedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");

                        using var scope = scopeFactory.CreateScope();
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        await bookingService.ConfirmBookingAsync(evt.BookingId, evt.ProcessedAt, stoppingToken);
                    }
                    // Места не удалось зарезервировать, отклоняем бронь
                    else if (eventType == nameof(EventSeatsReservationFailedMessage))
                    {
                        var evt = JsonSerializer.Deserialize<EventSeatsReservationFailedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");
                        using var scope = scopeFactory.CreateScope();
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        await bookingService.RejectBookingAsync(evt.BookingId, evt.ProcessedAt, stoppingToken);
                    }
                    // Места успешно освобождены, отменяем бронь
                    else if (eventType == nameof(EventSeatsReleasedMessage))
                    {
                        var evt = JsonSerializer.Deserialize<EventSeatsReleasedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");

                        using var scope = scopeFactory.CreateScope();
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        await bookingService.CancelBookingAsync(evt.BookingId, evt.ProcessedAt, stoppingToken);
                    }
                    // Места не удалось освободить, оставляем бронь как есть
                    else if (eventType == nameof(EventSeatsReleaseFailedMessage))
                    {
                        var evt = JsonSerializer.Deserialize<EventSeatsReleaseFailedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");
                    }
                    else
                    {
                        logger.LogWarning("Пропущено сообщение с неизвестным типом события: {EventType}", eventType);
                    }

                    // Сохраняем офсет в локальный буфер — не обращение к брокеру
                    // Консьюмер отправит накопленные офсеты в Kafka в фоне по таймеру
                    // При сбое до этой строки сообщение будет повторно доставлено (at-least-once)
                    consumer.Commit(consumeResult);
                    logger.LogInformation("Офсет сохранен в топик '{Topic}'...", Topics.Events);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                // Топик создастся автоматически после первой ошибки. Подписка заработает на следующем цикле Consume.
                catch (ConsumeException cex) when (cex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Топик {Topic} создастся автоматически", Topics.Events);
                }
                catch (Exception ex)
                {
                    // TODO: Отправка сообщения в DLQ (Dead Letter Queue)
                    logger.LogError(ex, "Ошибка при обработке сообщения. Отправка в DLQ.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Штатное завершение — CancellationToken был отменён хостом
            logger.LogInformation("Consumer остановлен штатно.");
        }
        finally
        {
            // Close() отправляет leave group — rebalance происходит немедленно,
            // без ожидания session.timeout.ms. Также коммитит буферизованные офсеты
            consumer.Close();
        }
    }
}

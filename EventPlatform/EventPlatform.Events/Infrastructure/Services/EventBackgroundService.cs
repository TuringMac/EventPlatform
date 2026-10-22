using Confluent.Kafka;
using EventPlatform.Contracts;
using EventPlatform.Contracts.Bookings;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Application.Options;
using EventPlatform.Events.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventPlatform.Events.Infrastructure.Services;

public class EventBackgroundService(IOptions<EventMessageBrokerOptions> options, ILogger<EventBackgroundService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
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
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        // Kafka назначит консьюмеру партиции в рамках перебалансировки группы
        consumer.Subscribe(Topics.Bookings);
        logger.LogInformation("Consumer запущен. Ожидание сообщений из топика '{Topic}'...", Topics.Bookings);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Блокирующий вызов: ждём следующего сообщения от брокера (pull-модель)
                    // При отмене CancellationToken бросает OperationCanceledException
                    var consumeResult = consumer.Consume(stoppingToken);

                    var headers = consumeResult.Message.Headers;
                    if (headers is null ||
                        !headers.TryGetLastBytes(options.Value.MessageTypeHeaderName, out var eventTypeBytes) ||
                        eventTypeBytes is null)
                    {
                        throw new InvalidOperationException($"В сообщении отсутствует заголовок {options.Value.MessageTypeHeaderName}.");
                    }

                    var eventType = System.Text.Encoding.UTF8.GetString(eventTypeBytes);

                    if (eventType == nameof(BookingCreateRequestedMessage))
                    {
                        var booking = JsonSerializer.Deserialize<BookingCreateRequestedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");

                        using var scope = scopeFactory.CreateScope();
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                        var eventMessageBrokerRepository = scope.ServiceProvider.GetRequiredService<IEventMessageBrokerRepository>();
                        await eventService.ReserveSeats(booking.EventId, booking.Id, stoppingToken);
                    }
                    else if (eventType == nameof(BookingCancelRequestedMessage))
                    {
                        var booking = JsonSerializer.Deserialize<BookingCancelRequestedMessage>(consumeResult.Message.Value)
                            ?? throw new InvalidOperationException("Не удалось десериализовать сообщение.");

                        using var scope = scopeFactory.CreateScope();
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                        var eventMessageBrokerRepository = scope.ServiceProvider.GetRequiredService<IEventMessageBrokerRepository>();
                        await eventService.ReleaseSeats(booking.EventId, booking.Id, stoppingToken);
                    }
                    else
                    {
                        logger.LogWarning("Пропущено сообщение с неизвестным типом события: {EventType}", eventType);
                    }

                    // Сохраняем офсет в локальный буфер — не обращение к брокеру
                    // Консьюмер отправит накопленные офсеты в Kafka в фоне по таймеру
                    // При сбое до этой строки сообщение будет повторно доставлено (at-least-once)
                    consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                // Топик создастся автоматически после первой ошибки. Подписка заработает на следующем цикле Consume.
                catch (ConsumeException cex) when (cex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogWarning("Топик {Topic} создастся автоматически", Topics.Bookings);
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

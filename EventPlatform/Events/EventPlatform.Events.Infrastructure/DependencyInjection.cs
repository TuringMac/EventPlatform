using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Infrastructure.DbContexts;
using EventPlatform.Events.Infrastructure.Options;
using EventPlatform.Events.Infrastructure.Repositories;
using EventPlatform.Events.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventPlatform.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // База данных
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Строка подключения 'Default' не найдена.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IEventMessageBrokerRepository, EventMessageBrokerRepository>();

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redis = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false, // Не выкидывает ошибку
                BacklogPolicy = BacklogPolicy.FailFast, // не копит команды
                ConnectTimeout = 1000,
                ConnectRetry = 0,
                AsyncTimeout = 500,
                SyncTimeout = 500
            };

            configurationOptions.EndPoints.Add(redis.Host, redis.Port);

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddScoped<EventRepository>();

        // Исключаем циклическую зависимость, используя фабрику для создания EventCachedRepository
        services.AddScoped<IEventRepository>(provider =>
            new EventCachedRepository(
                provider.GetRequiredService<EventRepository>(),
                provider.GetRequiredService<IConnectionMultiplexer>(),
                provider.GetRequiredService<IOptions<CacheOptions>>(),
                provider.GetRequiredService<ILogger<EventCachedRepository>>()));

        services.AddHostedService<EventBackgroundService>();

        return services;
    }
}

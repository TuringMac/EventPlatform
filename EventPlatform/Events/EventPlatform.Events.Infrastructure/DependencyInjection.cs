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

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IEventMessageBrokerRepository, EventMessageBrokerRepository>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConfig = configuration.GetSection("Redis");
            var configurationOptions = new ConfigurationOptions
            {
                EndPoints = { $"{redisConfig["Host"]}:{redisConfig["Port"]}" }
            };
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

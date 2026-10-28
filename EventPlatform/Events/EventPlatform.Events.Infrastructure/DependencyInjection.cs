using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Infrastructure.DbContexts;
using EventPlatform.Events.Infrastructure.Repositories;
using EventPlatform.Events.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHostedService<EventBackgroundService>();

        return services;
    }
}

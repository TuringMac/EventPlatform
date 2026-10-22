using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Application.Services;

namespace EventPlatform.Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Бизнес-логика
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}

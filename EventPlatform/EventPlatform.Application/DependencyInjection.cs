using EventPlatform.Application.Interfaces;
using EventPlatform.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Бизнес-логика
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}

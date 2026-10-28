using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventPlatform.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Бизнес-логика
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

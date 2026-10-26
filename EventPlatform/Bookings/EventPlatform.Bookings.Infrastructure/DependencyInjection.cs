using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Infrastructure.DbContexts;
using EventPlatform.Bookings.Infrastructure.Repositories;
using EventPlatform.Bookings.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventPlatform.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // База данных
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Строка подключения 'Default' не найдена.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IBookingMessageBrokerRepository, BookingMessageBrokerRepository>();

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}

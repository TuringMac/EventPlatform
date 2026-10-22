using EventPlatform.Users.Application.Interfaces;
using EventPlatform.Users.Application.Services;

namespace EventPlatform.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Бизнес-логика
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}

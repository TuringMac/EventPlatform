using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Services;

namespace EventPlatform.Api.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Бизнес-логика
            services.AddScoped<IEventService, EventService>();

            return services;
        }
    }
}

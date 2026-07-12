using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Storage;

namespace EventPlatform.Api.Infrastructure;

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // База данных
            services.AddSingleton<IEventStorage, InMemoryStorage>();

            return services;
        }
    }

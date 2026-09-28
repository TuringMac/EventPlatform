using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.DbContexts;
using EventPlatform.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace EventPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // База данных
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Строка подключения 'Default' не найдена.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                // Создаем пользователя-администратора при инициализации базы данных, если его нет
                .UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    var db = (AppDbContext)context;

                    var login = configuration["Admin:Username"]
                        ?? throw new InvalidOperationException("Логин администратора не настроен.");

                    var password = configuration["Admin:Password"]
                        ?? throw new InvalidOperationException("Пароль администратора не настроен.");

                    var exists = await db.Users
                        .AnyAsync(user => user.Login == login, cancellationToken);

                    if (exists)
                    {
                        return;
                    }

                    var passwordBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                    var passwordHash = Convert.ToHexString(passwordBytes);

                    db.Users.Add(new User(login, passwordHash, UserRoleEnum.Admin));
                    await db.SaveChangesAsync(cancellationToken);
                }));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}

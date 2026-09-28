using EventPlatform.Application.DTO;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EventPlatform.Application.Services;

internal class UserService(IUserRepository userRepository, ITokenGenerator tokenGenerator, ILogger<UserService> logger, IConfiguration configuration) : IUserService
{
    public async Task<string> GenerateJwtAsync(string login, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByLogin(login, cancellationToken);
        if (user == null || user.PasswordHash != password.ToHashString())
            throw new UnauthorizedAccessException("Неверный логин или пароль.");
        logger.LogInformation("Пользователь аутентифицирован: {UserId}, {Login}", user.Id, user.Login);

        var token = await tokenGenerator.GenerateToken(
            user,
            configuration["Jwt:Key"] ?? throw new InvalidOperationException("Ключ JWT не настроен."),
            int.Parse(configuration["Jwt:Lifetime"] ?? "15"), cancellationToken);
        logger.LogInformation("JWT сгенерирован для пользователя: {UserId}, {Login}", user.Id, login);

        return token;
    }

    public async Task<User> CreateAsync(UserRequest entity, CancellationToken cancellationToken)
    {
        var hashedPassword = entity.Password.ToHashString();
        var user = new User(entity.Login, hashedPassword, entity.Role);
        await userRepository.AddAsync(user, cancellationToken);
        logger.LogInformation("Пользователь создан: {UserId}, {Login}", user.Id, user.Login);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await userRepository.DeleteAsync(id, cancellationToken);
        logger.LogInformation("Пользователь удалён: {UserId}", id);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Пользователь с идентификатором {id} не найден.");
    }

    public async Task UpdateAsync(Guid id, UserRequest entity, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Пользователь с идентификатором {id} не найден.");
        var passwordHash = entity.Password.ToHashString();

        user.Update(entity.Login, passwordHash, entity.Role);
        await userRepository.UpdateAsync(id, user, cancellationToken);
        logger.LogInformation("Пользователь обновлён: {UserId}, {Login}", user.Id, user.Login);
    }
}

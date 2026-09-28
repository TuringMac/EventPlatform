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

internal class UserService(IUserRepository userRepository, ILogger<UserService> logger, IConfiguration configuration) : IUserService
{
    public async Task<string> GenerateJwtAsync(string login, string password, CancellationToken cancellationToken)
    {
        var hashedPassword = password.ToHashString();
        var user = await userRepository.GetUserByLoginHashedPassword(login, hashedPassword, cancellationToken);
        logger.LogInformation("Пользователь аутентифицирован: {UserId}, {Login}", user.Id, user.Login);

        // Создание списка утверждений
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Остальные необходимые утверждения
        };

        var jwtSecret = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Ключ JWT не настроен.");

        // Создание ключа и учётных данных для подписи
        var secretBytes = Encoding.UTF8.GetBytes(jwtSecret);
        if (secretBytes.Length < 32)
        {
            throw new InvalidOperationException($"Ключ JWT должен быть не менее 32 байт. Текущий размер: {secretBytes.Length} байт.");
        }
        var key = new SymmetricSecurityKey(secretBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Формирование объекта токена
        var token = new JwtSecurityToken(
            issuer: "EventPlatform.AuthServer",
            audience: "EventPlatform.Api",
            claims: claims,
            expires: DateTime.Now.AddMinutes(15),
            signingCredentials: creds
        );

        // Запись в строку и отправка клиенту
        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        logger.LogInformation("JWT сгенерирован для пользователя: {UserId}, {Login}", user.Id, user.Login);

        return accessToken;
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

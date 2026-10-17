using EventPlatform.Users.Domain.Exceptions;
using EventPlatform.Users.Application.DTO;
using EventPlatform.Users.Application.Interfaces;
using EventPlatform.Users.Domain.Model;

namespace EventPlatform.Users.Application.Services;

internal class UserService(IUserRepository userRepository, ITokenGenerator tokenGenerator, IMessageBrokerRepository messageBrokerRepository, ILogger<UserService> logger) : IUserService
{
    public async Task<string> GenerateJwtAsync(string login, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByLogin(login, cancellationToken);
        if (user == null || user.PasswordHash != password.ToHashString())
            throw new UnauthorizedAccessException("Неверный логин или пароль.");
        logger.LogInformation("Пользователь аутентифицирован: {UserId}, {Login}", user.Id, user.Login);

        var token = await tokenGenerator.GenerateToken(user, cancellationToken);
        logger.LogInformation("JWT сгенерирован для пользователя: {UserId}, {Login}", user.Id, user.Login);

        return token;
    }

    public async Task<User> CreateAsync(UserRequest entity, CancellationToken cancellationToken)
    {
        if (await userRepository.GetUserByLogin(entity.Login, cancellationToken) != null)
            throw new UserAlreadyExistsException($"Пользователь с логином {entity.Login} уже существует.");

        var hashedPassword = entity.Password.ToHashString();
        var user = new User(entity.Login, hashedPassword, entity.Role);
        await userRepository.AddAsync(user, cancellationToken);
        logger.LogInformation("Пользователь создан: {UserId}, {Login}", user.Id, user.Login);
        await messageBrokerRepository.UserCreatedMessageAsync(user.Id, cancellationToken);
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

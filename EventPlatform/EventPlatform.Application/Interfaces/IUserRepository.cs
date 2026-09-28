using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IUserRepository : IRepositoryCrud<User>
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User> GetUserByLoginHashedPassword(string login, string hashedPassword, CancellationToken cancellationToken);
}

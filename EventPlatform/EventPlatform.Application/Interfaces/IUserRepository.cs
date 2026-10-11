using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface IUserRepository : IRepositoryCrud<User>
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User?> GetUserByLogin(string login, CancellationToken cancellationToken);
}

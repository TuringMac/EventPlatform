using EventPlatform.Common.Application.Interfaces;
using EventPlatform.Users.Domain.Model;

namespace EventPlatform.Users.Application.Interfaces;

public interface IUserRepository : IRepositoryCrud<User>
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User?> GetUserByLogin(string login, CancellationToken cancellationToken);
}

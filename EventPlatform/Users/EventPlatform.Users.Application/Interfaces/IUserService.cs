using EventPlatform.Users.Application.DTO;
using EventPlatform.Users.Domain.Model;

namespace EventPlatform.Users.Application.Interfaces;

public interface IUserService
{
    Task<User> CreateAsync(UserRequest entity, CancellationToken cancellationToken);
    Task<string> GenerateJwtAsync(string login, string password, CancellationToken cancellationToken);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UserRequest entity, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

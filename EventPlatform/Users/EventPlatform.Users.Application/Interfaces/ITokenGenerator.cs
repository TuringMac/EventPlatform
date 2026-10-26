using EventPlatform.Users.Domain.Model;

namespace EventPlatform.Users.Application.Interfaces;

public interface ITokenGenerator
{
    Task<string> GenerateToken(User user, CancellationToken cancellationToken);
}

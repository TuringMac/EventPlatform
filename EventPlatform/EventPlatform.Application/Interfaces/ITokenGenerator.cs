using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface ITokenGenerator
{
    Task<string> GenerateToken(User user, CancellationToken cancellationToken);
}

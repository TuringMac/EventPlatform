using EventPlatform.Domain.Model;

namespace EventPlatform.Application.Interfaces;

public interface ITokenGenerator
{
    Task<string> GenerateToken(User user, string jwtKey, int lifetime, CancellationToken cancellationToken);
}

using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventPlatform.Infrastructure.Repositories;

public class TokenGenerator(IOptions<JwtOptions> jwtOptions) : ITokenGenerator
{
    const int requiredSecretLength = 32; // Минимальная длина ключа в байтах

    public async Task<string> GenerateToken(User user, CancellationToken cancellationToken)
    {
        // Создание списка утверждений
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Остальные необходимые утверждения
        };

        var jwtSecret = jwtOptions.Value.Key;

        // Создание ключа и учётных данных для подписи
        var secretBytes = Encoding.UTF8.GetBytes(jwtSecret);
        if (secretBytes.Length < requiredSecretLength)
        {
            throw new InvalidOperationException($"Ключ JWT должен быть не менее {requiredSecretLength} байт. Текущий размер: {secretBytes.Length} байт.");
        }
        var key = new SymmetricSecurityKey(secretBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Формирование объекта токена
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(jwtOptions.Value.Lifetime),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

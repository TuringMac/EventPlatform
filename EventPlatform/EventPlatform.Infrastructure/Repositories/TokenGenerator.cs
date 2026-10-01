using EventPlatform.Application;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventPlatform.Infrastructure.Repositories;

public class TokenGenerator : ITokenGenerator
{
    public async Task<string> GenerateToken(User user, string jwtKey, int lifetime, CancellationToken cancellationToken)
    {
        // Создание списка утверждений
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Остальные необходимые утверждения
        };

        var jwtSecret = jwtKey;

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
            expires: DateTime.Now.AddMinutes(lifetime),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

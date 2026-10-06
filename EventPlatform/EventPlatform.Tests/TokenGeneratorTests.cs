using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.Options;
using EventPlatform.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EventPlatform.Tests;

public class TokenGeneratorTests
{
    [Fact]
    public async Task GenerateToken_IncludesUserIdentityAndRole()
    {
        // Arrange
        const int keyLengthBytes = 32;
        const int tokenLifetimeMinutes = 30;
        const int allowedClockSkewMinutes = 5;
        var user = new User("admin", "hash", UserRoleEnum.Admin);
        var issuer = "EventPlatform.AuthServer";
        var audience = "EventPlatform.Api";
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = new string('x', keyLengthBytes),
            Issuer = issuer,
            Audience = audience,
            Lifetime = tokenLifetimeMinutes
        });
        var generator = new TokenGenerator(jwtOptions);

        // Act
        var token = await generator.GenerateToken(user, TestContext.Current.CancellationToken);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be(issuer);
        jwt.Audiences.Should().Contain(audience);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "admin");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes - allowedClockSkewMinutes));
    }

    [Fact]
    public async Task GenerateToken_WithShortKey_ThrowsInvalidOperationException()
    {
        // Arrange
        const int keyLengthBytes = 32;
        var user = new User("alice", "hash", UserRoleEnum.User);
        var jwtOptions = Options.Create(new JwtOptions { Key = new string('x', keyLengthBytes - 1) });
        var generator = new TokenGenerator(jwtOptions);

        // Act
        var act = () => generator.GenerateToken(user, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.Repositories;
using FluentAssertions;

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
        var generator = new TokenGenerator();

        // Act
        var token = await generator.GenerateToken(user, new string('x', keyLengthBytes), tokenLifetimeMinutes, TestContext.Current.CancellationToken);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
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
        const int tokenLifetimeMinutes = 30;
        var user = new User("alice", "hash", UserRoleEnum.User);
        var generator = new TokenGenerator();

        // Act
        var act = () => generator.GenerateToken(user, new string('x', keyLengthBytes - 1), tokenLifetimeMinutes, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

using EventPlatform.Application;
using EventPlatform.Application.DTO;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventPlatform.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly IUserService _service;

    public UserServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(_repository.Object);
        services.AddSingleton(_tokenGenerator.Object);
        _service = services.BuildServiceProvider().GetRequiredService<IUserService>();
    }

    [Fact]
    public async Task CreateAsync_HashesPasswordBeforeSaving()
    {
        // Arrange
        var request = new UserRequest { Login = "alice", Password = "secret", Role = UserRoleEnum.Admin };

        // Act
        var user = await _service.CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        user.Login.Should().Be(request.Login);
        user.Role.Should().Be(request.Role);
        user.PasswordHash.Should().Be(request.Password.ToHashString()).And.NotBe(request.Password);
        _repository.Verify(r => r.AddAsync(user, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GenerateJwtAsync_WithValidCredentials_DelegatesToTokenGenerator()
    {
        // Arrange
        var login = "alice";
        var jwtKey = "test-key";
        var lifetimeMinutes = 30;
        var user = new User(login, "secret".ToHashString(), UserRoleEnum.User);
        _repository.Setup(r => r.GetUserByLogin(login, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGenerator.Setup(g => g.GenerateToken(user, jwtKey, lifetimeMinutes, It.IsAny<CancellationToken>())).ReturnsAsync("jwt");

        // Act
        var token = await _service.GenerateJwtAsync(login, "secret", jwtKey, lifetimeMinutes, TestContext.Current.CancellationToken);

        // Assert
        token.Should().Be("jwt");
        _tokenGenerator.Verify(g => g.GenerateToken(user, jwtKey, lifetimeMinutes, TestContext.Current.CancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(false, "secret")]
    [InlineData(true, "wrong")]
    public async Task GenerateJwtAsync_WithUnknownUserOrWrongPassword_Rejects(bool userExists, string password)
    {
        // Arrange
        if (userExists)
            _repository.Setup(r => r.GetUserByLogin("alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User("alice", "secret".ToHashString(), UserRoleEnum.User));

        // Act
        var act = () => _service.GenerateJwtAsync("alice", password, "test-key", 30, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _tokenGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserWithHashedPassword()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = new User("alice", "old".ToHashString(), UserRoleEnum.User);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var request = new UserRequest { Login = "bob", Password = "new", Role = UserRoleEnum.Admin };

        // Act
        await _service.UpdateAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        user.Login.Should().Be("bob");
        user.Role.Should().Be(UserRoleEnum.Admin);
        user.PasswordHash.Should().Be("new".ToHashString());
        _repository.Verify(r => r.UpdateAsync(id, user, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = () => _service.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void ToHashString_UsesDeterministicSha256()
    {
        // Arrange
        var password = "abc";

        // Act
        var hash = password.ToHashString();

        // Assert
        hash.Should().Be("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
    }
}

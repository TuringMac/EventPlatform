using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.IntegrationTests;

[Collection("PostgreSql")]
public class UserRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AddAndGetUser_PersistLoginHashAndRole()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var user = new User("alice", "hash", UserRoleEnum.Admin);
        await using var arrangeContext = fixture.CreateContext();
        var arrangeRepository = new UserRepository(arrangeContext);
        await arrangeRepository.AddAsync(user, TestContext.Current.CancellationToken);

        // Act
        await using var verify = fixture.CreateContext();
        var repository = new UserRepository(verify);
        var loaded = await repository.GetUserByLogin("alice", TestContext.Current.CancellationToken);
        var all = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(user.Id);
        loaded.Role.Should().Be(UserRoleEnum.Admin);
        loaded.PasswordHash.Should().Be("hash");
        all.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateUser_PersistsChanges()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var user = new User("alice", "old", UserRoleEnum.User);
        await using var arrangeContext = fixture.CreateContext();
        var arrangeRepository = new UserRepository(arrangeContext);
        await arrangeRepository.AddAsync(user, TestContext.Current.CancellationToken);

        // Act
        await using (var context = fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            var loaded = await repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
            loaded!.Update("bob", "new", UserRoleEnum.Admin);
            await repository.UpdateAsync(user.Id, loaded, TestContext.Current.CancellationToken);
        }

        // Assert
        await using var verify = fixture.CreateContext();
        var verifyRepository = new UserRepository(verify);
        var updated = await verifyRepository.GetUserByLogin("bob", TestContext.Current.CancellationToken);
        updated.Should().NotBeNull();
        updated!.Role.Should().Be(UserRoleEnum.Admin);
        updated.PasswordHash.Should().Be("new");
    }

    [Fact]
    public async Task DeleteUser_RemovesUser()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var user = new User("alice", "hash", UserRoleEnum.User);
        await using var arrangeContext = fixture.CreateContext();
        var arrangeRepository = new UserRepository(arrangeContext);
        await arrangeRepository.AddAsync(user, TestContext.Current.CancellationToken);

        // Act
        await using var context = fixture.CreateContext();
        var repository = new UserRepository(context);
        await repository.DeleteAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        await using var verify = fixture.CreateContext();
        (await verify.Users.AnyAsync(u => u.Id == user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_DuplicateLogin_ThrowsDbUpdateException()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();
        var repository = new UserRepository(context);
        await repository.AddAsync(new User("alice", "hash", UserRoleEnum.User), TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.AddAsync(new User("alice", "other", UserRoleEnum.User), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}

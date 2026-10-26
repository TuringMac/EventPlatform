using EventPlatform.Common.Domain.Interfaces;
using EventPlatform.Contracts;

namespace EventPlatform.Users.Domain.Model;
public class User : IEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Login { get; private set; } = null!;
    public UserRoleEnum Role { get; private set; } = UserRoleEnum.User;
    public string PasswordHash { get; private set; } = null!;

    User() { }
    public User(string login, string passwordHash, UserRoleEnum role)
    {
        Login = login;
        Role = role;
        PasswordHash = passwordHash;
    }

    public void Update(string login, string passwordHash, UserRoleEnum role)
    {
        Login = login;
        Role = role;
        PasswordHash = passwordHash;
    }
}

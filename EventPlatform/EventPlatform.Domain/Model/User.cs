using EventPlatform.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Domain.Model;

public enum UserRoleEnum
{
    User,
    Admin,
}

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

    public List<Booking> Bookings { get; set; } = new();
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Application.DTO;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Role { get; set; }

    public UserResponse(Guid id, string login, string role)
    {
        Id = id;
        Login = login;
        Role = role;
    }
}

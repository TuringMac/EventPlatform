using EventPlatform.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Application.DTO;

public class UserRequest
{
    public string Login { get; set; }
    public string Password { get; set; }
    public UserRoleEnum Role { get; set; } = UserRoleEnum.User;
}

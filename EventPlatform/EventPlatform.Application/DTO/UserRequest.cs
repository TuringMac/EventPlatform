using EventPlatform.Domain.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EventPlatform.Application.DTO;

public class UserRequest
{
    public required string Login { get; set; }
    [MinLength(2, ErrorMessage = "Пароль должен быть как минимум 2 символа.")]
    public required string Password { get; set; }
    public UserRoleEnum Role { get; set; } = UserRoleEnum.User;
}

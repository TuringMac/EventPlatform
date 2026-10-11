using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Application.DTO;

public class LoginPasswordRequest
{
    [Required]
    public required string Login { get; set; }
    [Required]
    [MinLength(2, ErrorMessage = "Пароль должен быть как минимум 2 символа.")]
    public required string Password { get; set; }
}

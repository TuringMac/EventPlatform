using System.ComponentModel.DataAnnotations;

namespace EventPlatform.Application.DTO;

public class LoginPasswordRequest
{
    [Required]
    public string Login { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

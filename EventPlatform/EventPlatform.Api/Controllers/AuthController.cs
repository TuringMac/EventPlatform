using EventPlatform.Api.Mappers;
using EventPlatform.Application.DTO;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EventPlatform.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuthController(IUserService userService, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginPasswordRequest request, CancellationToken cancellationToken)
    {
        string accessToken = await userService.GenerateJwtAsync(
            request.Login, 
            request.Password,
            configuration["Jwt:Key"] ?? throw new InvalidOperationException("Ключ JWT не настроен."),
            int.Parse(configuration["Jwt:Lifetime"] ?? "15"),
            cancellationToken);

        return Ok(new { Token = accessToken });
    }
}

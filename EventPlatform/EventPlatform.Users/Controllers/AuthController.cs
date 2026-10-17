using EventPlatform.Users.Application.DTO;
using EventPlatform.Users.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Users.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuthController(IUserService userService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginPasswordRequest request, CancellationToken cancellationToken)
    {
        string accessToken = await userService.GenerateJwtAsync(request.Login, request.Password, cancellationToken);

        return Ok(new { Token = accessToken });
    }
}

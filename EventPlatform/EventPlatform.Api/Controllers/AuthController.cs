using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuthController(IUserService userService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        string accessToken = await userService.GenerateJwtAsync(request.Email, request.Password, cancellationToken);

        return Ok(new { Token = accessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

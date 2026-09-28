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

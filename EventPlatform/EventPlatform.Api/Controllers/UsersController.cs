using EventPlatform.Api.Mappers;
using EventPlatform.Application.DTO;
using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace EventPlatform.Api.Controllers;

[Authorize(Roles = nameof(UserRoleEnum.Admin))]
[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiBaseResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(new ApiResult<UserResponse>
        {
            Data = (await userService.GetByIdAsync(id, cancellationToken)).ToResponse(),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем пользователя по индексу из коллекции"
        });
    }

    [HttpGet]
    public async Task<ActionResult<ApiBaseResult>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(new ApiResult<IEnumerable<UserResponse>>
        {
            Data = (await userService.GetAllAsync(cancellationToken)).Select(u => u.ToResponse()),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем всех пользователей из коллекции"
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult>> Add(UserRequest value, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(value, cancellationToken);
        logger.LogDebug("DTO сконвертирован");
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем пользователя в коллекцию и возвращаем HTTP 201 Created"
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResult>> Update(Guid id, UserRequest value, CancellationToken cancellationToken)
    {
        await userService.UpdateAsync(id, value, cancellationToken);
        logger.LogDebug("Пользователь {Id} обновлен", id);

        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Обновляем данные пользователя в коллекции по индексу и возвращаем HTTP 204 No Content"
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResult>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);
        logger.LogDebug("Пользователь {Id} удален", id);

        return StatusCode((int)HttpStatusCode.NoContent, new ApiBaseResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Пользователь удален из базы"
        });
    }

    [AllowAnonymous]
    [HttpPost("~/api/auth/register")]
    public async Task<IActionResult> Register([FromBody] LoginPasswordRequest request, CancellationToken cancellationToken)
    {
        await userService.CreateAsync(new UserRequest
        {
            Login = request.Login,
            Password = request.Password,
            Role = Enum.Parse<UserRoleEnum>(request.Role ?? UserRoleEnum.User.ToString())
        }, cancellationToken);

        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Пользователь зарегистрирован"
        });
    }
}

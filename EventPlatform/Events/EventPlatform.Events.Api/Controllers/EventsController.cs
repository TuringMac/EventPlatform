using EventPlatform.Common.Api;
using EventPlatform.Contracts;
using EventPlatform.Events.Application.DTO;
using EventPlatform.Events.Application.Interfaces;
using EventPlatform.Events.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EventPlatform.Events.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService _eventService, ILogger<EventsController> _logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ApiResult<PaginatedResult<Event>>> Get(CancellationToken cancellationToken, string? title, DateTime? from, DateTime? to, int? page, int? pageSize)
    {
        return new ApiResult<PaginatedResult<Event>>
        {
            Data = await _eventService.GetAllAsync(cancellationToken, title, from, to, page, pageSize),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем все мероприятия из коллекции"
        };
    }

    [AllowAnonymous]
    [HttpGet("top")]
    public async Task<ActionResult<ApiResult<IEnumerable<Event>>>> GetTop10Events(CancellationToken cancellationToken)
    {
        var events = await _eventService.GetTop10EventsAsync(cancellationToken);
        return new ApiResult<IEnumerable<Event>>
        {
            Data = events,
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем топ 10 мероприятий из коллекции"
        };
    }

    /// <summary>
    /// Получить мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <returns></returns>
    /// <response code="200">Мероприятие найдено</response>
    /// <response code="404">Мероприятия нет в базе</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(ActionResult<Event>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //[ResponseCache(Duration = 60)]
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiBaseResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // В случае успеха возвращаем типизированный ответ с данными
        return new ApiResult<Event>
        {
            Data = await _eventService.GetByIdAsync(id, cancellationToken),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем мероприятие по индексу из коллекции"
        };
    }

    /// <summary>
    /// Получение мероприятия для обновления с чистыми данными из БД для целей администрирования.
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <returns></returns>
    /// <response code="200">Мероприятие найдено</response>
    /// <response code="404">Мероприятия нет в базе</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(ActionResult<Event>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpGet("{id:guid}/update")]
    public async Task<ActionResult<ApiBaseResult>> GetForUpdate(Guid id, CancellationToken cancellationToken)
    {
        return new ApiResult<Event>
        {
            Data = await _eventService.GetForUpdateAsync(id, cancellationToken),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем мероприятие по индексу из коллекции"
        };
    }

    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpPost]
    public async Task<ActionResult<ApiResult>> Add([FromBody] EventRequest value, CancellationToken cancellationToken)
    {
        var evt = await _eventService.CreateEventAsync(value, cancellationToken);
        _logger.LogDebug("DTO сконвертирован");
        return CreatedAtAction(nameof(GetById), new { id = evt.Id }, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем мероприятие в коллекцию и возвращаем HTTP 201 Created"
        });
    }

    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResult>> Update(Guid id, [FromBody] EventRequest value, CancellationToken cancellationToken)
    {
        await _eventService.UpdateAsync(id, value, cancellationToken);
        _logger.LogDebug("Событие {Id} обновлено", id);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Обновляем данные мероприятия в коллекции по индексу и возвращаем HTTP 204 No Content"
        });
    }

    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResult>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteAsync(id, cancellationToken);
        _logger.LogDebug("Событие {Id} удалено", id);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Мероприятие удалено из базы"
        });
    }
}

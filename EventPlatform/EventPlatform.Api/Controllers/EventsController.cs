using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EventPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService _eventService, IBookingService _bookingService, ILogger<EventsController> _logger) : ControllerBase
{
    // CancellationToken как заметка для себя, что так можно получить
    [HttpGet]
    public ApiResult<PaginatedResult<Event>> Get(CancellationToken cancellationToken, string? title, DateTime? from, DateTime? to, int? page, int? pageSize)
    {
        return new ApiResult<PaginatedResult<Event>>
        {
            Data = _eventService.GetAll(title, from, to, page, pageSize),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем все мероприятия из коллекции"
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
    [HttpGet("{id:guid}")]
    public ActionResult<ApiBaseResult> GetById(Guid id)
    {
        // В случае успеха возвращаем типизированный ответ с данными
        return new ApiResult<Event>
        {
            Data = _eventService.GetById(id),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем мероприятие по индексу из коллекции"
        };
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult>> Post([FromBody] EventDto value, CancellationToken cancellationToken = default)
    {
        var evt = await _eventService.CreateEventAsync(
            value.Id,
            value.Title,
            value.Description ?? string.Empty,
            value.StartAt,
            value.EndAt,
            value.TotalSeats,
            cancellationToken
        );
        _logger.LogDebug("DTO сконвертирован");
        _eventService.Add(evt);
        return CreatedAtAction(nameof(GetById), new { id = evt.Id }, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем мероприятие в коллекцию и возвращаем HTTP 201 Created"
        });
    }

    /// <summary>
    /// Забронировать места на мероприятие
    /// </summary>
    /// <param name="eventId">Идентификатор мероприятия</param>
    /// <returns></returns>
    /// <response code="409">Нет доступных мест на мероприятие</response>
    [HttpPost("{eventId:guid}/book")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResult>> CreateBooking(Guid eventId, CancellationToken cancellationToken)
    {
        var book = await _bookingService.CreateBookingAsync(eventId, cancellationToken);
        return AcceptedAtAction(
            nameof(BookingsController.GetById),
            "Bookings",
            new { id = book.Id }, new ApiResult<Booking>
            {
                Data = book,
                Success = true,
                StatusCode = HttpStatusCode.Accepted,
                Message = "Бронирование взято в обработку"
            });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResult>> Put(Guid id, [FromBody] EventDto value)
    {
        var evt = _eventService.GetById(id);
        evt.Title = value.Title;
        evt.Description = value.Description;
        evt.StartAt = value.StartAt;
        evt.EndAt = value.EndAt;

        _eventService.Update(id, evt);
        _logger.LogDebug("Событие {Id} обновлено", evt.Id);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Обновляем данные мероприятия в коллекции по индексу и возвращаем HTTP 204 No Content"
        });
    }

    [HttpDelete("{id:guid}")]
    public ActionResult<ApiResult> Delete(Guid id)
    {
        _eventService.Delete(id);
        _logger.LogDebug("Событие {Id} удалено", id);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Мероприятие удалено из базы"
        });
    }
}

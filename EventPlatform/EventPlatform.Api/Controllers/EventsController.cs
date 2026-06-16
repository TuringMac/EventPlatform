using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EventPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService _eventService) : ControllerBase
{
    [ResponseCache(Duration = 60)]
    [HttpGet]
    public ApiResult<IEnumerable<Event>> Get(CancellationToken cancellationToken)
    {
        return new ApiResult<IEnumerable<Event>>
        {
            Data = _eventService.GetAll(),
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
    [HttpGet("{id:guid}")]
    public ApiBaseResult GetById(Guid id)
    {
        // Пытаемся получить мероприятие по индексу из коллекции
        try
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
        // Исключение означает, что значение индекса, переданного в метод, находится
        // вне диапазона допустимых значений списка, а значит, значение здания не удалось найти
        catch (KeyNotFoundException ex)
        {
            // В случае ошибки возвращаем неуспешный результат со статусом Not Found
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти мероприятие по индексу: {ex.Message}"
            };
        }
    }

    // QUESTION: Стандартизированный результат это же всегда HTTP 200 и без Location заголовка?
    [HttpPost]
    public ApiResult Post([FromBody] EventDto value)
    {
        try
        {
            if (!TryValidateModel(value))
            {
                return new ApiResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = $"Не удалось добавить мероприятие"
                };
                //return BadRequest(ModelState);
            }

            var evt = new Event
            {
                Id = value.Id,
                Title = value.Title,
                Description = value.Description,
                StartAt = value.StartAt,
                EndAt = value.EndAt,
            };

            _eventService.Add(evt);
            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.Created,
                Message = "Добавляем мероприятие в коллекцию и возвращаем HTTP 201 Created"
            };
        }
        catch (Exception ex)
        {
            // В случае ошибки возвращаем неуспешный результат со статусом Not Found
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.BadRequest,
                Message = $"Не удалось добавить мероприятие: {ex.Message}"
            };
        }
        // QUESTION: Так как же лучше? Created сам добавит заголовок Location
        //return Created(nameof(GetById), new { id = value.Id }, value);
    }

    // QUESTION: Как быть, если в URL один Id, а в теле value.Id другой?
    [HttpPut("{id:guid}")]
    public ApiResult Put(Guid id, [FromBody] EventDto value)
    {

        try
        {
            if (!TryValidateModel(value))
            {
                return new ApiResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = $"Не удалось добавить мероприятие"
                };
                //return BadRequest(ModelState);
            }

            var evt = new Event
            {
                Id = value.Id,
                Title = value.Title,
                Description = value.Description,
                StartAt = value.StartAt,
                EndAt = value.EndAt,
            };

            _eventService.Update(id, evt);

            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Обновляем данные мероприятия в коллекции по индексу и возвращаем HTTP 204 No Content"
            };
        }
        catch (KeyNotFoundException ex)
        {
            // В случае ошибки возвращаем неуспешный результат со статусом Not Found
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти мероприятие по индексу: {ex.Message}"
            };
        }
    }

    [HttpDelete("{id:guid}")]
    public ApiResult Delete(Guid id)
    {
        try
        {
            _eventService.Delete(id);
            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Мероприятие удалено из базы"
            };
        }
        catch (KeyNotFoundException ex)
        {
            // В случае ошибки возвращаем неуспешный результат со статусом Not Found
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти мероприятие по индексу: {ex.Message}"
            };
        }
    }
}

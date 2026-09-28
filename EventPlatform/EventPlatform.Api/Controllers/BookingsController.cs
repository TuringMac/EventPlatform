using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace EventPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingsController(IBookingService _bookingService) : ControllerBase
{
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiBaseResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        Booking booking = null!;
        if (User.IsInRole(nameof(UserRoleEnum.Admin)))
            // Админ видит все брони
            booking = await _bookingService.GetBookingByIdAsync(id, cancellationToken);
        else if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            // Не админ видит только свои брони
            booking = await _bookingService.GetBookingByIdAsync(id, currentUserId, cancellationToken);
        }
        else
            return Forbid();

        return Ok(new ApiResult<Booking>
        {
            Data = booking,
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем бронирование по индексу из коллекции"
        });
    }

    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpGet("~/api/users/{userId:guid}/bookings")]
    public async Task<ActionResult<ApiBaseResult>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
            return Forbid();

        return Ok(new ApiResult<IEnumerable<Booking>>
        {
            Data = await _bookingService.GetBookingsByUserIdAsync(userId, cancellationToken),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем бронирования пользователя"
        });
    }

    /// <summary>
    /// Забронировать места на мероприятие
    /// </summary>
    /// <param name="eventId">Идентификатор мероприятия</param>
    /// <returns></returns>
    /// <response code="409">Нет доступных мест на мероприятие</response>
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize]
    [HttpPost("~/api/events/{eventId:guid}/book")]
    public async Task<ActionResult<ApiResult>> CreateBooking(Guid eventId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
            return Forbid();
        var userId = currentUserId;
        var book = await _bookingService.CreateBookingAsync(eventId, userId, cancellationToken);
        return AcceptedAtAction(
            nameof(GetById),
            new { id = book.Id },
            new ApiResult<Booking>
            {
                Data = book,
                Success = true,
                StatusCode = HttpStatusCode.Accepted,
                Message = "Бронирование взято в обработку"
            });
    }

    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpDelete("{bookingId:guid}")]
    public async Task<ActionResult<ApiResult>> CancelBookingById(Guid bookingId, CancellationToken cancellationToken)
    {
        await _bookingService.CancelBookingByIdAsync(bookingId, cancellationToken);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Бронирование отменено"
        });
    }

    [Authorize]
    [HttpDelete("~/api/events/{eventId:guid}/book")]
    public async Task<ActionResult<ApiResult>> CancelBooking(Guid eventId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
            return Forbid();
        var userId = currentUserId;
        var book = await _bookingService.CancelBookingAsync(eventId, userId, cancellationToken);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Бронирование отменено"
        });
    }
}

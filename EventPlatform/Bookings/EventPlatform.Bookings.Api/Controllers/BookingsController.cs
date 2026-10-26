using EventPlatform.Bookings.Application.Interfaces;
using EventPlatform.Bookings.Domain.Model;
using EventPlatform.Common;
using EventPlatform.Common.Api;
using EventPlatform.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace EventPlatform.Bookings.Controllers.Api;

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
        var book = await _bookingService.CreateBookingAsync(
            eventId,
            userId,
            cancellationToken);
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

    [Authorize]
    [HttpDelete("{bookingId:guid}")]
    public async Task<ActionResult<ApiResult>> CancelBookingById(Guid bookingId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !TryGetUserRole(out var userRole))
            return Forbid();

        await _bookingService.CancelBookingRequestByIdAsync(bookingId, userId, userRole == UserRoleEnum.Admin, cancellationToken);
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
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !TryGetUserRole(out var userRole))
            return Forbid();

        await _bookingService.CancelBookingRequestAsync(eventId, userId, userRole == UserRoleEnum.Admin, cancellationToken);
        return StatusCode((int)HttpStatusCode.NoContent, new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Бронирование отменено"
        });
    }

    /// <summary>
    /// Список броней мероприятия
    /// </summary>
    /// <param name="eventId">Идентификатор мероприятия</param>
    /// <returns></returns>
    /// <response code="409">Нет доступных мест на мероприятие</response>
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    [HttpGet("{eventId:guid}/bookings")]
    public async Task<ActionResult<ApiResult>> GetEventBookings(Guid eventId, CancellationToken cancellationToken)
    {
        var evt = await _bookingService.GetByEventIdAsync(eventId, cancellationToken);
        return Ok(new ApiResult<IEnumerable<Guid>>
        {
            Data = evt.Select(b => b.Id),
            Success = true,
            StatusCode = HttpStatusCode.Accepted,
            Message = "Список броней для мероприятия " + eventId
        });
    }

    private bool TryGetUserRole(out UserRoleEnum userRole)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse(roleClaim, out userRole)
            && Enum.IsDefined(userRole)
            && roleClaim == userRole.ToString();
    }
}

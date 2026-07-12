using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EventPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingsController(IBookingService _bookingService, ILogger<BookingsController> _logger) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiBaseResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(new ApiResult<Booking>
        {
            Data = await _bookingService.GetBookingByIdAsync(id, cancellationToken),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем бронирование по индексу из коллекции"
        });
    }
}

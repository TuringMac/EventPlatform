using EventPlatform.Application.Exceptions;
using EventPlatform.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int status = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NoAvailableSeatsException => StatusCodes.Status409Conflict,
            EventEndedException => StatusCodes.Status400BadRequest,
            BookingLimitReachedException => StatusCodes.Status409Conflict,
            BookingAlreadyCancelledException => StatusCodes.Status409Conflict,
            UserAlreadyExistsException => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Внутренняя ошибка. Метод: {Method}, путь: {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Ошибка запроса ({Status}): {Error}. Метод: {Method}, путь: {Path}",
                status, exception.Message, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = "An error occurred",
            Type = exception.GetType().Name,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        // 3. Write your payload to the HTTP response stream
        //httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // 4. Return true to signal that this exception is handled 
        // Returning false would pass the error to the next registered handler
        return true;
    }
}

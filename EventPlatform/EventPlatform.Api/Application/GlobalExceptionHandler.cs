using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Api.Application;

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
        // 1. Log the unhandled runtime error
        _logger.LogError(exception,
                "Unhandled exception. Method: {Method}, Path: {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

        // 2. Format a standardized RFC-compliant error payload
        int status = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

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

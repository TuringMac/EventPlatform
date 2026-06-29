using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventPlatform.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> _logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Вызываем следующий middleware в конвейере
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception. Method: {Method}, Path: {Path}",
                context.Request.Method,
                context.Request.Path
                );
            int status = ex switch
            {
                ArgumentException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Title = "An error occurred",
                Type = ex.GetType().Name,
                Detail = ex.Message,
                Instance = context.Request.Path,
            };
            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }
    }
}

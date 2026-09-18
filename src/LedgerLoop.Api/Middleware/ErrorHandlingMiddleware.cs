using System.Text.Json;
using LedgerLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedgerLoop.Api.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var problem = Translate(exception);

            _logger.Log(
                problem.Status >= 500 ? LogLevel.Error : LogLevel.Warning,
                exception,
                "Request {Method} {Path} failed with {Status}",
                context.Request.Method,
                context.Request.Path,
                problem.Status);

            context.Response.Clear();
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

    private static ProblemDetails Translate(Exception exception) => exception switch
    {
        DomainValidationException => new ProblemDetails
        {
            Title = "Invalid document",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest
        },
        DocumentNotFoundException => new ProblemDetails
        {
            Title = "Not found",
            Detail = exception.Message,
            Status = StatusCodes.Status404NotFound
        },
        ForbiddenOperationException => new ProblemDetails
        {
            Title = "Not permitted",
            Detail = exception.Message,
            Status = StatusCodes.Status403Forbidden
        },
        _ => new ProblemDetails
        {
            Title = "Unexpected failure",
            Detail = "The request could not be completed.",
            Status = StatusCodes.Status500InternalServerError
        }
    };
}

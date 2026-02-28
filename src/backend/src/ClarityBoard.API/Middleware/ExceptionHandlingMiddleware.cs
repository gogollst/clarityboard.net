using System.Text.Json;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Response already started, cannot write error response: {Message}", exception.Message);
            return;
        }

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Extensions = { ["errors"] = validationEx.Errors },
            }),

            NotFoundException => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = exception.Message,
            }),

            ForbiddenException => (StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = exception.Message,
            }),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = exception.Message,
            }),

            DomainException domainEx => (StatusCodes.Status422UnprocessableEntity, new ProblemDetails
            {
                Title = "Domain Rule Violation",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = domainEx.Message,
                Extensions = { ["code"] = domainEx.Code },
            }),

            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, new ProblemDetails
            {
                Title = "Business Rule Violation",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = exception.Message,
            }),

            _ => (StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred.",
            }),
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Request error ({StatusCode}): {Message}", statusCode, exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

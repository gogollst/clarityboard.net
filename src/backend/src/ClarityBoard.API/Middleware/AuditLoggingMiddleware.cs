using System.Security.Claims;
using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.API.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!AuditedMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Capture request body
        context.Request.EnableBuffering();
        string? requestBody = null;

        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Capture original response stream
        var originalBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            // Always restore the original response body, even if an exception occurs.
            // Without this, a disposed MemoryStream would prevent the ExceptionHandlingMiddleware
            // from writing proper error responses (causing silent 500s instead of 400s).
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }

        // Only audit successful mutations
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            try
            {
                var auditService = context.RequestServices.GetService<IAuditService>();
                if (auditService is null) return;

                var userId = context.User.FindFirstValue("userId");
                var entityId = context.User.FindFirstValue("entity_id");
                var path = context.Request.Path.Value ?? "";

                // Extract table/resource from path (e.g., /api/accounting -> accounting)
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var tableName = segments.Length >= 2 ? segments[1] : "unknown";

                await auditService.LogAsync(
                    entityId: entityId is not null ? Guid.Parse(entityId) : null,
                    action: context.Request.Method,
                    tableName: tableName,
                    recordId: segments.Length >= 3 ? segments[2] : null,
                    oldValues: null,
                    newValues: requestBody,
                    userId: userId is not null ? Guid.Parse(userId) : null,
                    ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                    userAgent: context.Request.Headers.UserAgent.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
        }
    }
}

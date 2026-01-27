using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace DecisionReplay.API.Middleware;

/// <summary>
/// Global Exception Handler for Production
/// 
/// Purpose:
/// - Catch all unhandled exceptions
/// - Log errors with context
/// - Return user-friendly error responses
/// - Hide internal implementation details
/// - Provide consistent error format
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception with full details
        _logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}, User: {User}",
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User.Identity?.Name ?? "Anonymous"
        );

        // Determine status code and user-facing message
        var (statusCode, errorCode, message) = MapExceptionToResponse(exception);

        // Create error response using standardized format
        var errorResponse = new DecisionReplay.API.DTOs.ErrorResponse(
            errorCode,
            message,
            httpContext.Request.Path.Value
        );

        // Only include stack trace in development
        if (!IsProductionEnvironment())
        {
            errorResponse.Details = new Dictionary<string, object>
            {
                ["stackTrace"] = exception.ToString()
            };
        }

        // Write response
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse),
            cancellationToken
        );

        return true; // Exception handled
    }

    private (int statusCode, string errorCode, string message) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Forbidden, "Forbidden", "You do not have permission to access this resource."),

            KeyNotFoundException or ArgumentException =>
                ((int)HttpStatusCode.BadRequest, "BadRequest", exception.Message),

            InvalidOperationException when exception.Message.Contains("GEMINI_API_KEY") =>
                ((int)HttpStatusCode.ServiceUnavailable, "ServiceUnavailable", "AI reasoning service is temporarily unavailable."),

            HttpRequestException when exception.Message.Contains("Gemini") =>
                ((int)HttpStatusCode.BadGateway, "ExternalServiceError", "AI reasoning service is experiencing issues. Please try again later."),

            TaskCanceledException or OperationCanceledException =>
                ((int)HttpStatusCode.RequestTimeout, "Timeout", "The request took too long to complete. Please try again."),

            _ =>
                ((int)HttpStatusCode.InternalServerError, "InternalServerError", "An unexpected error occurred. Our team has been notified.")
        };
    }

    private bool IsProductionEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env != null && env.Equals("Production", StringComparison.OrdinalIgnoreCase);
    }
}

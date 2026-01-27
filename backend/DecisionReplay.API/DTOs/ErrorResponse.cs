namespace DecisionReplay.API.DTOs;

/// <summary>
/// Standardized error response format for all API endpoints
/// Ensures consistent error handling across the application
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Path { get; set; }
    public Dictionary<string, object>? Details { get; set; }

    public ErrorResponse(string error, string message, string? path = null)
    {
        Error = error;
        Message = message;
        Timestamp = DateTime.UtcNow;
        Path = path;
    }

    // Factory methods for common error types
    public static ErrorResponse BadRequest(string message, string? path = null)
        => new("BadRequest", message, path);

    public static ErrorResponse Unauthorized(string message, string? path = null)
        => new("Unauthorized", message, path);

    public static ErrorResponse Forbidden(string message, string? path = null)
        => new("Forbidden", message, path);

    public static ErrorResponse NotFound(string message, string? path = null)
        => new("NotFound", message, path);

    public static ErrorResponse InternalServerError(string message, string? path = null)
        => new("InternalServerError", message, path);

    public static ErrorResponse ServiceUnavailable(string message, string? path = null)
        => new("ServiceUnavailable", message, path);

    public static ErrorResponse RateLimitExceeded(string message, Dictionary<string, object> details, string? path = null)
        => new("RateLimitExceeded", message, path) { Details = details };

    public static ErrorResponse ValidationError(string message, Dictionary<string, object>? details = null, string? path = null)
        => new("ValidationError", message, path) { Details = details };
}

namespace DecisionReplay.API.DTOs;

/// <summary>
/// Standardized success response for operations that don't return data
/// </summary>
public class SuccessResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public SuccessResponse(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }

    public static SuccessResponse Create(string message) => new(message);
}

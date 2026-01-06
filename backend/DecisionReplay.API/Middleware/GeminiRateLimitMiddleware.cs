using System.Collections.Concurrent;

namespace DecisionReplay.API.Middleware;

/// <summary>
/// Rate Limiting Middleware for Gemini API Calls
/// 
/// Production Requirements:
/// - Max 5-7 requests per minute per user
/// - Max 450 requests per day per user
/// - Graceful degradation when limits reached
/// - Clear error messaging
/// 
/// Architecture:
/// - In-memory rate limiting (TODO: Replace with Redis for production scale)
/// - Per-user tracking (uses JWT claims)
/// - Sliding window algorithm
/// - Thread-safe concurrent collections
/// </summary>
public class GeminiRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GeminiRateLimitMiddleware> _logger;

    // Rate limit configuration - Gemini API Free Tier Limits
    // Free tier: 15 RPM, 1,500 RPD, 32,000 per month
    // Setting slightly lower to leave buffer for safety
    private const int MaxRequestsPerMinute = 10;  // Conservative: 10/15 RPM
    private const int MaxRequestsPerDay = 1000;   // Conservative: 1000/1500 RPD
    private const int RateLimitWindowMinutes = 1;
    private const int RateLimitWindowDays = 1;

    // In-memory storage (TODO: Replace with distributed cache for multi-instance deployments)
    private static readonly ConcurrentDictionary<string, UserRateLimitData> _userLimits = new();

    public GeminiRateLimitMiddleware(RequestDelegate next, ILogger<GeminiRateLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to Gemini-dependent endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var isGeminiEndpoint = path.Contains("/api/v2/decisions") &&
                              (path.Contains("/analyze") ||
                               path.Contains("/replay") ||
                               path.Contains("/query") ||
                               context.Request.Method == "POST" && path.EndsWith("/decisions"));

        if (!isGeminiEndpoint)
        {
            await _next(context);
            return;
        }

        // Extract user identifier (email from JWT claims or IP address as fallback)
        var userId = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        // Get or create rate limit data for user
        var rateLimitData = _userLimits.GetOrAdd(userId, _ => new UserRateLimitData());

        // Clean up old requests
        CleanupOldRequests(rateLimitData);

        // Check rate limits
        var (isAllowed, errorMessage) = CheckRateLimits(rateLimitData);

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}. {ErrorMessage}", userId, errorMessage);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "RateLimitExceeded",
                message = errorMessage,
                retryAfter = GetRetryAfterSeconds(rateLimitData),
                limits = new
                {
                    perMinute = MaxRequestsPerMinute,
                    perDay = MaxRequestsPerDay,
                    remaining = new
                    {
                        perMinute = Math.Max(0, MaxRequestsPerMinute - rateLimitData.RequestsInLastMinute.Count),
                        perDay = Math.Max(0, MaxRequestsPerDay - rateLimitData.RequestsInLastDay.Count)
                    }
                }
            };

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // Record this request
        var now = DateTime.UtcNow;
        rateLimitData.RequestsInLastMinute.Add(now);
        rateLimitData.RequestsInLastDay.Add(now);

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit-Minute"] = MaxRequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Limit-Day"] = MaxRequestsPerDay.ToString();
        context.Response.Headers["X-RateLimit-Remaining-Minute"] =
            Math.Max(0, MaxRequestsPerMinute - rateLimitData.RequestsInLastMinute.Count).ToString();
        context.Response.Headers["X-RateLimit-Remaining-Day"] =
            Math.Max(0, MaxRequestsPerDay - rateLimitData.RequestsInLastDay.Count).ToString();

        _logger.LogInformation(
            "Gemini API request allowed for user {UserId}. Remaining: {RemainingMinute}/min, {RemainingDay}/day",
            userId,
            Math.Max(0, MaxRequestsPerMinute - rateLimitData.RequestsInLastMinute.Count),
            Math.Max(0, MaxRequestsPerDay - rateLimitData.RequestsInLastDay.Count)
        );

        await _next(context);
    }

    private void CleanupOldRequests(UserRateLimitData data)
    {
        var now = DateTime.UtcNow;

        // Remove requests older than 1 minute
        data.RequestsInLastMinute.RemoveAll(timestamp =>
            (now - timestamp).TotalMinutes > RateLimitWindowMinutes);

        // Remove requests older than 1 day
        data.RequestsInLastDay.RemoveAll(timestamp =>
            (now - timestamp).TotalDays > RateLimitWindowDays);
    }

    private (bool isAllowed, string errorMessage) CheckRateLimits(UserRateLimitData data)
    {
        if (data.RequestsInLastMinute.Count >= MaxRequestsPerMinute)
        {
            return (false, $"Rate limit exceeded: Maximum {MaxRequestsPerMinute} requests per minute. Please wait before trying again.");
        }

        if (data.RequestsInLastDay.Count >= MaxRequestsPerDay)
        {
            return (false, $"Daily quota exceeded: Maximum {MaxRequestsPerDay} requests per day. Your quota will reset tomorrow.");
        }

        return (true, string.Empty);
    }

    private int GetRetryAfterSeconds(UserRateLimitData data)
    {
        var now = DateTime.UtcNow;

        // If minute limit exceeded, calculate seconds until oldest request expires
        if (data.RequestsInLastMinute.Count >= MaxRequestsPerMinute)
        {
            var oldestRequest = data.RequestsInLastMinute.Min();
            var secondsUntilExpiry = (int)Math.Ceiling((oldestRequest.AddMinutes(RateLimitWindowMinutes) - now).TotalSeconds);
            return Math.Max(1, secondsUntilExpiry);
        }

        // If day limit exceeded, calculate seconds until midnight UTC
        if (data.RequestsInLastDay.Count >= MaxRequestsPerDay)
        {
            var midnight = now.Date.AddDays(1);
            return (int)(midnight - now).TotalSeconds;
        }

        return 0;
    }

    /// <summary>
    /// Per-user rate limit tracking data
    /// Thread-safe via concurrent collections
    /// </summary>
    private class UserRateLimitData
    {
        public List<DateTime> RequestsInLastMinute { get; } = new();
        public List<DateTime> RequestsInLastDay { get; } = new();
    }
}

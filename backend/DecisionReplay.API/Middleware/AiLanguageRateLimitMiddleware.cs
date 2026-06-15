using System.Collections.Concurrent;
using System.Security.Claims;

namespace DecisionReplay.API.Middleware;

public sealed class AiLanguageRateLimitMiddleware
{
    private const int RequestsPerMinute = 20;
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> Requests = new();

    public AiLanguageRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!UsesLanguageInterface(context.Request))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        var queue = Requests.GetOrAdd(userId, _ => new Queue<DateTime>());
        var now = DateTime.UtcNow;
        var allowed = true;

        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromMinutes(1))
                queue.Dequeue();
            if (queue.Count >= RequestsPerMinute)
                allowed = false;
            else
                queue.Enqueue(now);
        }

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "RateLimitExceeded",
                message = "Too many language-analysis requests. Try again in one minute."
            });
            return;
        }

        await _next(context);
    }

    private static bool UsesLanguageInterface(HttpRequest request) =>
        request.Path.StartsWithSegments("/api/v2/decisions") &&
        request.Method == HttpMethods.Post;
}

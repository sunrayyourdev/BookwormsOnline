using BookwormsOnline.Services;

namespace BookwormsOnline.Middleware;

public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;

    public SessionActivityMiddleware(RequestDelegate next, ILogger<SessionActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserSessionService sessionService)
    {
        // Only track for authenticated users
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.Session.Id;
            
            // Update last active timestamp (fire and forget to avoid blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await sessionService.UpdateLastActiveAsync(sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating session activity");
                }
            });
        }

        await _next(context);
    }
}

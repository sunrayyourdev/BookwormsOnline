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
        // Continue processing the request
        await _next(context);

        // Update session activity after response (non-blocking for user)
        if (context.User?.Identity?.IsAuthenticated == true && context.Response.StatusCode < 400)
        {
            var sessionId = context.Session.Id;
            
            // Update in background without blocking response
            _ = Task.Run(async () =>
            {
                try
                {
                    await sessionService.UpdateLastActiveAsync(sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating session activity for session {SessionId}", sessionId);
                }
            });
        }
    }
}

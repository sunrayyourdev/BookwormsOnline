using BookwormsOnline.Services;

namespace BookwormsOnline.Middleware;

public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SessionActivityMiddleware(
        RequestDelegate next, 
        ILogger<SessionActivityMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Continue processing the request
        await _next(context);

        // Update session activity after response (non-blocking for user)
        if (context.User?.Identity?.IsAuthenticated == true && context.Response.StatusCode < 400)
        {
            var sessionId = context.Session.Id;
            
            // Update in background without blocking response, using proper scope
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var sessionService = scope.ServiceProvider.GetRequiredService<IUserSessionService>();
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

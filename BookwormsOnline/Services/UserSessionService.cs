using BookwormsOnline.Data;
using BookwormsOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace BookwormsOnline.Services;

public class UserSessionService : IUserSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(ApplicationDbContext context, ILogger<UserSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> CreateSessionAsync(string userId, string sessionId, string ipAddress, string userAgent)
    {
        try
        {
            var session = new UserSession
            {
                UserId = userId,
                SessionId = sessionId,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent ?? "Unknown",
                CreatedDate = DateTime.UtcNow,
                LastActiveDate = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Session created for user {userId} with session ID {sessionId}");
            return session.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating session for user {userId}");
            throw;
        }
    }

    public async Task UpdateLastActiveAsync(string sessionId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsRevoked);

            if (session != null)
            {
                session.LastActiveDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating last active for session {sessionId}");
            // Don't throw - this is a non-critical operation
        }
    }

    public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
    {
        try
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .OrderByDescending(s => s.LastActiveDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving sessions for user {userId}");
            return new List<UserSession>();
        }
    }

    public async Task RevokeSessionAsync(int sessionId, string userId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session != null)
            {
                session.IsRevoked = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Session {sessionId} revoked for user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revoking session {sessionId} for user {userId}");
            throw;
        }
    }

    public async Task RevokeAllOtherSessionsAsync(string currentSessionId, string userId)
    {
        try
        {
            await _context.UserSessions
                .Where(s => s.UserId == userId && s.SessionId != currentSessionId && !s.IsRevoked)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsRevoked, true));

            _logger.LogInformation($"All other sessions revoked for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revoking all other sessions for user {userId}");
            throw;
        }
    }
}

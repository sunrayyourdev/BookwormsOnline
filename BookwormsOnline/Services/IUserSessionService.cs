namespace BookwormsOnline.Services;

public interface IUserSessionService
{
    Task<int> CreateSessionAsync(string userId, string sessionId, string ipAddress, string userAgent);
    Task UpdateLastActiveAsync(string sessionId);
    Task<List<BookwormsOnline.Models.UserSession>> GetActiveSessionsAsync(string userId);
    Task RevokeSessionAsync(int sessionId, string userId);
    Task RevokeAllOtherSessionsAsync(string currentSessionId, string userId);
}

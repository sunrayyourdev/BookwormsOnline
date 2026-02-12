using BookwormsOnline.Models;

namespace BookwormsOnline.Services;

public interface IAuditLogService
{
    Task LogAsync(string? userId, string email, string action, string? ipAddress);
}

using BookwormsOnline.Data;
using BookwormsOnline.Models;

namespace BookwormsOnline.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string email, string action, string? ipAddress)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Email = email,
            Action = action,
            Timestamp = DateTime.UtcNow,
            IPAddress = ipAddress
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

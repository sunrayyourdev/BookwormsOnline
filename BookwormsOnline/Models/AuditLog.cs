using System;

namespace BookwormsOnline.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IPAddress { get; set; }
}

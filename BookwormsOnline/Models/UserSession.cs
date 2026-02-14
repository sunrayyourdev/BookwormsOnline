using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Models;

public class UserSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastActiveDate { get; set; }

    public bool IsRevoked { get; set; }

    // Navigation property
    public ApplicationUser? User { get; set; }
}

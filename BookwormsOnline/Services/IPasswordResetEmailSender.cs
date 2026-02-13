using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookwormsOnline.Services;

/// <summary>
/// Extended email sender interface that includes password reset specific functionality.
/// Extends IEmailSender to add SendResetLinkAsync method for password reset emails.
/// </summary>
public interface IPasswordResetEmailSender : IEmailSender
{
    /// <summary>
    /// Sends a password reset email with the reset link to the user.
    /// </summary>
    /// <param name="email">The email address to send the reset link to</param>
    /// <param name="callbackUrl">The password reset callback URL</param>
    Task SendResetLinkAsync(string email, string callbackUrl);
}


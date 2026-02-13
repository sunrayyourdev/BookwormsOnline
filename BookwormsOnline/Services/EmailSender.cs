using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Security.Cryptography;
using System.Text;

namespace BookwormsOnline.Services;

public class EmailSender : IPasswordResetEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            var mailSettings = _configuration.GetSection("MailSettings");
            var smtpHost = mailSettings["SmtpHost"];
            var smtpPort = int.Parse(mailSettings["SmtpPort"] ?? "2525");
            var smtpUsername = Environment.GetEnvironmentVariable("MAIL_USERNAME");
            var smtpPassword = Environment.GetEnvironmentVariable("MAIL_PASSWORD");
            var fromEmail = mailSettings["FromEmail"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError(
                    "Email credentials not configured. Please set MAIL_USERNAME and MAIL_PASSWORD environment variables.");
                throw new InvalidOperationException("Email service is not configured with required credentials.");
            }

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "BookwormsOnline"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email successfully sent with subject {Subject}",
                    subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with subject {Subject}", subject);
            throw;
        }
    }

    /// <summary>
    /// Sends a password reset email with the reset link to the user.
    /// This method encapsulates HTML body construction to prevent taint-path issues
    /// where untrusted data could be exposed in email content.
    /// </summary>
    /// <param name="email">The email address to send the reset link to</param>
    /// <param name="callbackUrl">The password reset callback URL</param>
    public async Task SendResetLinkAsync(string email, string callbackUrl)
    {
        try
        {
            // Construct the HTML body with proper encoding of the callback URL
            // This ensures the URL is safely encoded within the email HTML context
            var encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);
            var htmlMessage = $"Please reset your password by <a href='{encodedUrl}'>clicking here</a>.";

            await SendEmailAsync(email, "Reset Password", htmlMessage);

            _logger.LogInformation("Password reset email sent to {Email}", RedactEmail(email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", RedactEmail(email));
            throw;
        }
    }

    /// <summary>
    /// Returns a non-sensitive, deterministic representation of an email address
    /// suitable for logging. The returned value is a SHA-256 hash and cannot be
    /// used to reconstruct the original email.
    /// </summary>
    /// <param name="email">The email address to redact.</param>
    /// <returns>A hashed representation of the email, or "N/A" if none.</returns>
    private static string RedactEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "N/A";
        }

        // Normalize the email to ensure consistent hashing
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var inputBytes = Encoding.UTF8.GetBytes("email:" + normalizedEmail);

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(inputBytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return "emailHash:" + sb.ToString();
        }
    }
}

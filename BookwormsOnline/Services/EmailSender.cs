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

    /// <summary>
    /// Redacts an email address for safe logging by producing a non-reversible token.
    /// Example: user@example.com becomes email:[ABC12345]
    /// </summary>
    /// <param name="email">The email address to redact</param>
    /// <returns>
    /// A non-reversible representation of the email address suitable for logs,
    /// or a placeholder if input is null/empty/invalid.
    /// </returns>
    private string RedactEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "email:[unknown]";
        }

        try
        {
            // Use a one-way hash so the original email cannot be reconstructed
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(email);
                var hashBytes = sha256.ComputeHash(bytes);
                var hashString = Convert.ToBase64String(hashBytes);

                // Use only a prefix to keep log messages compact while still allowing correlation
                var prefixLength = Math.Min(12, hashString.Length);
                var hashPrefix = hashString.Substring(0, prefixLength);

                return $"email:[{hashPrefix}]";
            }
        }
        catch
        {
            // In the unlikely event hashing fails, avoid leaking the raw email
            return "email:[redacted]";
        }
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
                _logger.LogInformation("Email successfully sent to {Email} with subject {Subject}", RedactEmail(email),
                    subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", RedactEmail(email), subject);
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
}

using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;

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
    public async Task SendResetLinkAsync(string email, string callbackUrl)
    {
        try
        {
            var encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);
            var htmlMessage = $"Please reset your password by <a href='{encodedUrl}'>clicking here</a>.";

            await SendEmailAsync(email, "Reset Password", htmlMessage);

            _logger.LogInformation("Password reset email sent to");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
            throw;
        }
    }
}

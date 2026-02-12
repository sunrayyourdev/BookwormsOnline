using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace BookwormsOnline.Services;

public class EmailSender : IEmailSender
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
                _logger.LogError("Email credentials not configured. Please set MAIL_USERNAME and MAIL_PASSWORD environment variables.");
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
                _logger.LogInformation("Email successfully sent to {Email} with subject {Subject}", email, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", email, subject);
            throw;
        }
    }
}

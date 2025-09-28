using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _server;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;

        var smtpSection = configuration.GetSection("SmtpSettings");
        _server = smtpSection["Server"] ?? throw new InvalidOperationException("SMTP server is not configured.");
        _senderEmail = smtpSection["SenderEmail"] ?? throw new InvalidOperationException("SMTP sender email is not configured.");
        _senderName = smtpSection["SenderName"] ?? throw new InvalidOperationException("SMTP sender name is not configured.");
        _username = smtpSection["Username"] ?? throw new InvalidOperationException("SMTP username is not configured.");
        _password = smtpSection["Password"] ?? throw new InvalidOperationException("SMTP password is not configured.");
        _port = smtpSection.GetValue("Port", 587);
        _enableSsl = smtpSection.GetValue("EnableSsl", true);
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string toName, string otpCode, CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_senderEmail, _senderName),
            Subject = "Your Lumina password reset code",
            Body = BuildPasswordResetBody(toName, otpCode),
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(toEmail, toName));

        using var client = new SmtpClient(_server, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = _enableSsl
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Sent password reset OTP email to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            throw;
        }
    }

    private static string BuildPasswordResetBody(string name, string otpCode)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
        return $"{greeting},\n\n" +
               "We received a request to reset the password for your Lumina account. " +
               $"Use the following verification code to continue: {otpCode}.\n\n" +
               "If you did not request this code, you can safely ignore this email.";
    }
}
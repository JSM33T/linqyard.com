using System.Net;
using System.Net.Mail;
using System.Text;
using Linqyard.Infra.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linqyard.Infra
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
                client.EnableSsl = _smtpSettings.EnableSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);

                using var message = new MailMessage();
                message.From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;
                message.BodyEncoding = Encoding.UTF8;
                message.SubjectEncoding = Encoding.UTF8;

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
                throw;
            }
        }

        public async Task SendVerificationEmailAsync(string to, string firstName, string verificationCode)
        {
            var subject = "Verify Your Linkyard Account";
            var body = GenerateVerificationEmailHtml(firstName, verificationCode);
            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendPasswordResetEmailAsync(string to, string firstName, string resetCode)
        {
            var subject = "Reset Your Linkyard Password";
            var body = GeneratePasswordResetEmailHtml(firstName, resetCode);
            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendWelcomeEmailAsync(string to, string firstName)
        {
            var subject = "Welcome to Linkyard!";
            var body = GenerateWelcomeEmailHtml(firstName);
            await SendEmailAsync(to, subject, body, true);
        }

        private string GenerateVerificationEmailHtml(string firstName, string verificationCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify Your Account</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .code {{ background: #e8f4fd; border: 2px dashed #1976d2; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px; }}
        .code-text {{ font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1976d2; font-family: 'Courier New', monospace; }}
        .button {{ display: inline-block; background: #1976d2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to Linkyard!</h1>
        </div>
        <div class='content'>
            <h2>Hi {firstName}!</h2>
            <p>Thanks for joining Linkyard! To complete your account setup, please verify your email address using the code below:</p>
            
            <div class='code'>
                <p><strong>Your Verification Code:</strong></p>
                <div class='code-text'>{verificationCode}</div>
            </div>
            
            <p>This code will expire in <strong>15 minutes</strong> for security reasons.</p>
            
            <p>If you didn't create an account with Linkyard, you can safely ignore this email.</p>
            
            <p>Welcome aboard!<br>
            The Linkyard Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 Linkyard. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailHtml(string firstName, string resetCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reset Your Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .code {{ background: #ffe8e8; border: 2px dashed #d32f2f; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px; }}
        .code-text {{ font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #d32f2f; font-family: 'Courier New', monospace; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔒 Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hi {firstName}!</h2>
            <p>We received a request to reset your Linkyard account password. Use the code below to reset your password:</p>
            
            <div class='code'>
                <p><strong>Your Reset Code:</strong></p>
                <div class='code-text'>{resetCode}</div>
            </div>
            
            <div class='warning'>
                <p><strong>⚠️ Security Notice:</strong></p>
                <ul>
                    <li>This code will expire in <strong>15 minutes</strong></li>
                    <li>Don't share this code with anyone</li>
                    <li>If you didn't request this reset, please ignore this email</li>
                </ul>
            </div>
            
            <p>If you didn't request a password reset, your account is still secure and no action is needed.</p>
            
            <p>Best regards,<br>
            The Linkyard Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 Linkyard. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateWelcomeEmailHtml(string firstName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Linkyard!</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #2ecc71 0%, #27ae60 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .features {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .feature {{ margin: 15px 0; }}
        .button {{ display: inline-block; background: #27ae60; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎊 Welcome to Linkyard!</h1>
        </div>
        <div class='content'>
            <h2>Hi {firstName}!</h2>
            <p>Congratulations! Your email has been verified and your Linkyard account is now active.</p>
            
            <div class='features'>
                <h3>🚀 Get Started:</h3>
                <div class='feature'>✨ Complete your profile</div>
                <div class='feature'>👥 Connect with friends</div>
                <div class='feature'>💬 Start conversations</div>
                <div class='feature'>🌟 Explore communities</div>
            </div>
            
            <p>We're excited to have you as part of the Linkyard community!</p>
            
            <p>Happy connecting!<br>
            The Linkyard Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 Linkyard. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

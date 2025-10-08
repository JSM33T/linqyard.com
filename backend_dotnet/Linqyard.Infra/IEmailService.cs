namespace Linqyard.Infra
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendVerificationEmailAsync(string to, string firstName, string verificationCode);
        Task SendPasswordResetEmailAsync(string to, string firstName, string resetCode);
        Task SendWelcomeEmailAsync(string to, string firstName);
    }
}

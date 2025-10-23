namespace Linqyard.Infra
{
    /// <summary>
    /// Defines the contract for email-related operations within the Linqyard infrastructure.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message asynchronously.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body content of the email message.</param>
        /// <param name="isHtml">Indicates whether the body content is in HTML format. Defaults to <c>true</c>.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Sends a verification email to a user containing a verification code.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="firstName">The recipient's first name.</param>
        /// <param name="verificationCode">The unique verification code.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendVerificationEmailAsync(string to, string firstName, string verificationCode);

        /// <summary>
        /// Sends a password reset email containing a reset code.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="firstName">The recipient's first name.</param>
        /// <param name="resetCode">The unique password reset code.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendPasswordResetEmailAsync(string to, string firstName, string resetCode);

        /// <summary>
        /// Sends a welcome email to a newly registered user.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="firstName">The recipient's first name.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendWelcomeEmailAsync(string to, string firstName);
    }
}

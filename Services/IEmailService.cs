using BeautySalon.Services;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace BeautySalon.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string callbackUrl);
        Task SendEmailConfirmationAsync(string toEmail, string callbackUrl);
        Task SendChangeEmailConfirmationAsync(string toEmail, string callbackUrl);
        Task SendAdminNotificationAsync(string userEmail, string userName);
        Task SendRegistrationApprovedEmailAsync(string toEmail, string callbackUrl);
    }
}
using Azure.Core;
using BeautySalon.Models;
using BeautySalon.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

using System.Security.Policy;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string callbackUrl)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("boxerlionmms@gmail.com", "MK");
        var subject = "Գաղտնաբառի վերականգնում";
        var to = new EmailAddress(toEmail);
        var plainTextContent = $"Գաղտնաբառը վերականգնելու համար սեղմեք հետևյալ հղումը: {callbackUrl}";
        var htmlContent = $"<strong>Գաղտնաբառը վերականգնելու համար սեղմեք <a href='{callbackUrl}'>այստեղ</a>:</strong>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string callbackUrl)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("boxerlionmms@gmail.com", "MK");
        var subject = "Հաստատեք ձեր էլ. փոստի հասցեն";
        var to = new EmailAddress(toEmail);
        var plainTextContent = $"Խնդրում ենք հաստատել ձեր հաշիվը՝ սեղմելով հետևյալ հղումը. {callbackUrl}";
        var htmlContent = $"<strong>Խնդրում ենք հաստատել ձեր հաշիվը՝ <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>սեղմելով այստեղ</a>:</strong>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
    }

    public async Task SendChangeEmailConfirmationAsync(string toEmail, string callbackUrl)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("boxerlionmms@gmail.com", "MK");
        var subject = "Հաստատեք ձեր նոր էլ. փոստի հասցեն";
        var to = new EmailAddress(toEmail);
        var plainTextContent = $"Խնդրում ենք հաստատել ձեր նոր էլ. փոստի հասցեն՝ սեղմելով հետևյալ հղումը: {callbackUrl}";
        var htmlContent = $"<strong>Խնդրում ենք հաստատել ձեր նոր էլ. փոստի հասցեն՝ <a href='{callbackUrl}'>սեղմելով այստեղ</a>:</strong>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
    }


    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("boxerlionmms@gmail.com", "MK");
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
        await client.SendEmailAsync(msg);
    }


    public async Task SendAdminNotificationAsync(string userEmail, string userName)
    {
        var adminEmail = _configuration["AdminEmail"];
        var subject = "New User Registration Request";
        var token = Guid.NewGuid().ToString();

        var baseUrl = _configuration["AppSettings:BaseUrl"];
        var approveUrl = $"{baseUrl}/Admin/ApproveUser?email={Uri.EscapeDataString(userEmail)}&fullName={Uri.EscapeDataString(userName)}&token={token}";

        var plainTextContent = $"A new user has requested registration:\n\nName: {userName}\nEmail: {userEmail}\n\nTo approve this request, copy and paste this URL into your browser: {approveUrl}";

        var htmlContent = $@"
        <h2>New User Registration Request</h2>
        <p>A new user has requested registration:</p>
        <p><strong>Name:</strong> {HtmlEncoder.Default.Encode(userName)}</p>
        <p><strong>Email:</strong> {HtmlEncoder.Default.Encode(userEmail)}</p>
        <p>To approve this request, <a href='{HtmlEncoder.Default.Encode(approveUrl)}'>click here</a>.</p>";

        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress("boxerlionmms@gmail.com", "MK"),
            new EmailAddress(adminEmail),
            subject,
            plainTextContent,
            htmlContent);

        var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
        var response = await client.SendEmailAsync(msg);
    }


    public async Task SendRegistrationApprovedEmailAsync(string toEmail, string callbackUrl)
    {
        var subject = "Ձեր գրանցումը հաստատվել է";
        var message = $"Ձեր գրանցման հայտը հաստատվել է: Խնդրում ենք հաստատել ձեր էլ. փոստը՝ սեղմելով այս հղումը: <a href='{callbackUrl}'>Հաստատել էլ. փոստը</a>";
        await SendEmailAsync(toEmail, subject, message);
    }
}
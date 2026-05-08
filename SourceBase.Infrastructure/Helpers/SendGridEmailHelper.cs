using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Infrastructure.Helpers;

public class SendGridEmailHelper(AppSettings appSettings) : IEmailHelper
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var client = new SendGridClient(appSettings.SendGridApiKey);
        var fromEmail = new EmailAddress(appSettings.SendGridAccountOwner);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, body, body);
        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiInternalException("Failed to send email");
        }
    }
}

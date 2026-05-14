using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Api.Common;

namespace SourceBase.Api.Infrastructure.Helpers;

public class SendGridEmailHelper(AppSettings appSettings, ILogger<SendGridEmailHelper> logger)
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Sending email to {To} with subject {Subject} and body {Body}", to, subject, body);

        if (string.IsNullOrEmpty(appSettings.SendGridApiKey))
        {
            logger.LogError("SendGrid API key is not configured.");
            return;
        }

        var client = new SendGridClient(appSettings.SendGridApiKey);
        var fromEmail = new EmailAddress(appSettings.SendGridAccountOwner);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, body, body);
        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to send email to {To} with subject {Subject} and body {Body}", to, subject, body);
            throw new ApiInternalException("Failed to send email");
        }
    }
}

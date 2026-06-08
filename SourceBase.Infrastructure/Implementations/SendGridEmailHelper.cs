using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Domain.Entities;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SendGrid.Helpers.Errors.Model;

namespace SourceBase.Infrastructure.Implementations;

public class SendGridEmailHelper(AppSettings appSettings, ILogger<SendGridEmailHelper> logger, IDbContext dbContext) : IEmailHelper
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);

        dbContext.Emails.Add(new EmailEntity { To = to, Subject = subject, Body = body });
        await dbContext.SaveChangesAsync(default);

        if (string.IsNullOrEmpty(appSettings.SendGridApiKey))
        {
            logger.LogWarning("SendGrid API key is not configured. Email saved to database but not dispatched.");
            return;
        }

        var client = new SendGridClient(appSettings.SendGridApiKey);
        var fromEmail = new EmailAddress(appSettings.SendGridAccountOwner);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, body, body);
        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to send email to {To} with subject {Subject}", to, subject);
            throw new BadRequestException("Failed to send email");
        }
    }
}

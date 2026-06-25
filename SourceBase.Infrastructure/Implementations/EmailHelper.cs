using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain.Entities;

namespace SourceBase.Infrastructure.Implementations;

public class EmailHelper(IDbContext dbContext, AppSettings appSettings, ILogger<EmailHelper> logger) : IEmailHelper
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);

        var email = new EmailEntity { To = to, Subject = subject, Body = body, SentOn = DateTime.UtcNow };
        dbContext.Emails.Add(email);
        await dbContext.SaveChangesAsync(default);

        if (string.IsNullOrEmpty(appSettings.SendGridApiKey))
        {
            logger.LogWarning("SendGrid API key not configured. Email to {To} skipped.", to);
            return;
        }

        var client = new SendGridClient(appSettings.SendGridApiKey);
        var from = new EmailAddress(appSettings.SendGridAccountOwner);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);
        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Email sent to {To} via SendGrid", to);
            return;
        }

        var detail = await response.Body.ReadAsStringAsync();
        logger.LogError("SendGrid rejected email to {To}: {Status} — {Detail}", to, response.StatusCode, detail);
        throw new InvalidOperationException($"SendGrid error: {response.StatusCode}");
    }
}

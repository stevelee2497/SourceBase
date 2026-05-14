namespace SourceBase.Api.Infrastructure.Interfaces;

public interface IEmailHelper
{
    Task SendEmailAsync(string to, string subject, string body);
}
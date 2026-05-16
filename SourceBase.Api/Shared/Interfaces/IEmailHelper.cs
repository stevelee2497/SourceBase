namespace SourceBase.Api.Shared.Interfaces;

public interface IEmailHelper
{
    Task SendEmailAsync(string to, string subject, string body);
}
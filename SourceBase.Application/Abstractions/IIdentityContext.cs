namespace SourceBase.Application.Abstractions;

public interface IIdentityContext
{
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
}
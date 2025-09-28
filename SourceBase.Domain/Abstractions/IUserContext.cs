namespace SourceBase.Domain.Abstractions;

public interface IUserContext
{
    Guid CurrentUserId { get; }
    Task LoginAsync(string email, string password);
    Task RefreshAsync(string refreshToken);
}

namespace SourceBase.Api.Infrastructure.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }

    string UserEmail { get; }
}
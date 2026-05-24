namespace SourceBase.Api.Shared.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }

    string UserEmail { get; }

    string UserName { get; }

    string[] Roles { get; }
}
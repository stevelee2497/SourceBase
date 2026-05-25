namespace SourceBase.Api.Shared.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }

    string? UserName { get; }

    string? Email { get; }

    string[] Roles { get; }
}
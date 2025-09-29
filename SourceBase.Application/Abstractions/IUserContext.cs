namespace SourceBase.Application.Abstractions;

public interface IUserContext
{
    Guid UserId { get; }
    string UserEmail { get; }
}

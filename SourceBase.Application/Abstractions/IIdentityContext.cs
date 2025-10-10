using SourceBase.Domain.Entities;

namespace SourceBase.Application.Abstractions;

public interface IIdentityContext
{
    Task GenerateTokenAsync(UserEntity user);
    Task RefreshTokenAsync(string refreshToken);
}
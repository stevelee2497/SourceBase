using SourceBase.Domain.Entities;

namespace SourceBase.Application.Abstractions;

public interface IIdentityContext
{
    // Auth
    Task ValidateAndSignInAsync(string email, string password);
    Task RefreshTokenAsync(string refreshToken);

    // User management
    Task CreateUserAsync(string email, string password);
    Task ConfirmEmailAsync(string email, string code, string role);
    Task ResetPasswordAsync(string email, string code, string newPassword);

    // Token generation (returns Base64Url-encoded tokens)
    Task<string> GenerateEmailConfirmationTokenAsync(string email);
    Task<string> GeneratePasswordResetTokenAsync(string email);

    // User info
    Task<UserEntity?> GetUserWithRolesAsync(Guid userId, CancellationToken ct = default);
    Task UpdateUserInfoAsync(Guid userId, string? firstName, string? lastName, CancellationToken ct = default);
}
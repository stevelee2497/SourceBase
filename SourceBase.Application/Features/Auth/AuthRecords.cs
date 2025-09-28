using System.ComponentModel.DataAnnotations;

namespace SourceBase.Application.Features.Auth;

public record RegisterRequest([Required][EmailAddress] string Email, [Required] string Password);

public record LoginRequest([Required] string Email, [Required] string Password);

public record RefreshTokenRequest([Required] string Token);

public record UserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

public record UserInfoUpdateRequest(string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);
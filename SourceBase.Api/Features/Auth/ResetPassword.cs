using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ResetPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/resetPassword", Handler).WithTags("Auth").AllowAnonymous();

    private async Task<NoContent> Handler([FromBody] ResetPasswordRequest request, UserManager<UserEntity> userManager, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(request.Code));
        var result = await userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
        if (!result.Succeeded)
            throw new ApiInternalException(result.Errors.First().Description);

        return TypedResults.NoContent();
    }
}

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

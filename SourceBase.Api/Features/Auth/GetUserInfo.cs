using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class GetUserInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/auth/info", Handler).WithTags("Auth");

    private async Task<Ok<GetUserInfoResponse>> Handler(UserManager<UserEntity> userManager, ICurrentUser currentUser, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        var roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(new GetUserInfoResponse(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, roles));
    }
}

public record GetUserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

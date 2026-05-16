using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Common;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Auth;

public class GetUserInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/auth/info", Handler).WithTags("Auth");

    private async Task<Ok<GetUserInfoResponse>> Handler(UserManager<ApplicationUser> userManager, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        var roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(new GetUserInfoResponse(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, roles));
    }
}

public record GetUserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

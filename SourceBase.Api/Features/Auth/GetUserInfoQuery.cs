using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Identity;

namespace SourceBase.Api.Features.Auth;

public record GetUserInfoQuery() : IRequest<UserInfoResponse>;

public class GetUserInfoQueryHandler(UserManager<ApplicationUser> userManager, CurrentUser currentUser) : IRequestHandler<GetUserInfoQuery, UserInfoResponse>
{
    public async Task<UserInfoResponse> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        var roles = await userManager.GetRolesAsync(user);
        return new UserInfoResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            roles);
    }
}

public record UserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

public static class GetUserInfoQueryEndpoint
{
    public static IEndpointRouteBuilder MapGetUserInfoQueryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/info", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetUserInfoQuery(), cancellationToken)))
            .WithTags("Auth");

        return endpoints;
    }
}

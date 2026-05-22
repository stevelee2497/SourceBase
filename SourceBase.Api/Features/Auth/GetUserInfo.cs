using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record GetUserInfoRequest;

public record GetUserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

public class GetUserInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/auth/info", (GetUserInfoHandler handler, CancellationToken ct) => handler.Handle(new GetUserInfoRequest(), ct))
        .WithTags("Auth");
}

public class GetUserInfoHandler(UserManager<UserEntity> userManager, ICurrentUser currentUser) : IRequestHandler<GetUserInfoRequest, GetUserInfoResponse>
{
    public async Task<GetUserInfoResponse> Handle(GetUserInfoRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        var roles = await userManager.GetRolesAsync(user);
        return new GetUserInfoResponse(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, roles);
    }
}

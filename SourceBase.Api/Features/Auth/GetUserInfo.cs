using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record GetUserInfoRequest;

public record GetUserInfoResponse(Guid Id, string? UserName, string? Email, bool EmailConfirmed, string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);

public class GetUserInfoEndpoint : IEndpoint
{
    public const string Route = "auth/info";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetUserInfoHandler handler, CancellationToken ct) => handler.Handle(new GetUserInfoRequest(), ct))
        .WithTags("Auth");
}

public class GetUserInfoHandler(ICurrentUser currentUser, IDbContext dbContext) : IRequestHandler<GetUserInfoRequest, GetUserInfoResponse>
{
    public async Task<GetUserInfoResponse> Handle(GetUserInfoRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FindAsync([currentUser.UserId], ct);
        return new GetUserInfoResponse(
            Id: user!.Id,
            UserName: user.UserName,
            Email: user.Email,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Roles: currentUser.Roles
        );
    }
}

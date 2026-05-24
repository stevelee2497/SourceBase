using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record GetUserInfoRequest;

public record GetUserInfoResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);

public class GetUserInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/auth/info", (GetUserInfoHandler handler, CancellationToken ct) => handler.Handle(new GetUserInfoRequest(), ct))
        .WithTags("Auth");
}

public class GetUserInfoHandler(ICurrentUser currentUser, IDbContext dbContext) : IRequestHandler<GetUserInfoRequest, GetUserInfoResponse>
{
    public async Task<GetUserInfoResponse> Handle(GetUserInfoRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUser.UserId)
            .Select(x => new GetUserInfoResponse(
                x.Id,
                x.UserName,
                x.Email,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                x.UserRoles.Select(ur => ur.Role.Name!).ToList()))
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException();

        return user;
    }
}

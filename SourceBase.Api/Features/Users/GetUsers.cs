using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

public record GetUsersRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, UsersOrder? OrderBy = UsersOrder.CreatedOn) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record UserResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, bool EmailConfirmed, IEnumerable<string> Roles);

public class GetUsersEndpoint : IEndpoint
{
    public const string Route = "users";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetUsersRequest request, GetUsersHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class GetUsersHandler(IDbContext dbContext) : IRequestHandler<GetUsersRequest, PagingResponse<UserResponse>>
{
    public async Task<PagingResponse<UserResponse>> Handle(GetUsersRequest request, CancellationToken ct)
    {
        var users = await dbContext.Users.PaginateAsync(
            selector: user => new UserResponse(
                Id: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                EmailConfirmed: user.EmailConfirmed,
                Roles: user.Roles.Select(r => r.Name!)
            ),
            paging: request,
            ct: ct
        );

        return users;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UsersOrder
{
    UserName,
    Email,
    FirstName,
    LastName,
    PhoneNumber,
    EmailConfirmed,
    CreatedOn,
    CreatedBy,
    UpdatedOn,
    UpdatedBy
}

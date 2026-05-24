using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

public record GetUsersRequest(int? Page = 1, int? Limit = 10, PagingOrder? Order = PagingOrder.Asc, UsersOrder? OrderBy = UsersOrder.CreatedOn) : PagingRequest(Page, Limit, Order, OrderBy?.ToString());

public record UserResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, bool EmailConfirmed, IEnumerable<string> Roles);

public class GetUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet("/users", ([AsParameters] GetUsersRequest request, GetUsersHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class GetUsersHandler(IDbContext dbContext) : IRequestHandler<GetUsersRequest, PagingResponse<UserResponse>>
{
    public async Task<PagingResponse<UserResponse>> Handle(GetUsersRequest request, CancellationToken ct)
    {
        var query = dbContext.Users.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(request.Direction, request.Order)
            .Skip(((request.Page ?? 1) - 1) * (request.Limit ?? 10))
            .Take(request.Limit ?? 10)
            .Select(user => new UserResponse(
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.EmailConfirmed,
                user.UserRoles.Select(ur => ur.Role.Name!).ToList()))
            .ToListAsync(ct);

        return new PagingResponse<UserResponse>(items, request.Page ?? 1, request.Limit ?? 10, total);
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

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SourceBase.Api.Features.Auth;

public class UpdateUserInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPut("/auth/info", Handler).WithTags("Auth");

    private async Task<NoContent> Handler([FromBody] UpdateUserInfoRequest request, IDbContext dbContext, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken) ?? throw new NotFoundException();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}

public record UpdateUserInfoRequest(string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);

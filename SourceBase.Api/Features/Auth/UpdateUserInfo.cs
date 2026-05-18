using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class UpdateUserInfo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPut("/auth/info", Handler).WithTags("Auth");

    private async Task<NoContent> Handler([FromBody] UpdateUserInfoRequest request, IDbContext dbContext, ICurrentUser currentUser, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct) ?? throw new NotFoundException();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}

public record UpdateUserInfoRequest(string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public class UpdateUserInfoRequestValidator : AbstractValidator<UpdateUserInfoRequest>
{
    public UpdateUserInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
    }
}

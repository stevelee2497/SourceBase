using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;

namespace SourceBase.Application.Features.Auth;

public record UpdateUserInfoRequest(string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl, Guid? DefaultTodoListId);

public record UpdateUserInfoResponse(Guid Id);

public class UpdateUserInfoEndpoint : IEndpoint
{
    public const string Route = "auth/info";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateUserInfoRequest request, UpdateUserInfoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Auth");
}

public class UpdateUserInfoHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<UpdateUserInfoRequest, UpdateUserInfoResponse>
{
    public async Task<UpdateUserInfoResponse> Handle(UpdateUserInfoRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct) ?? throw new NotFoundException();
        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
        user.DefaultTodoListId = request.DefaultTodoListId ?? user.DefaultTodoListId;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserInfo.WithId(currentUser.UserId), ct);
        return new UpdateUserInfoResponse(user.Id);
    }
}

public class UpdateUserInfoRequestValidator : AbstractValidator<UpdateUserInfoRequest>
{
    public UpdateUserInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
    }
}

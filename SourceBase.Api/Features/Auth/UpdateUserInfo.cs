using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record UpdateUserInfoRequest(string? FirstName, string? LastName, string? PhoneNumber);

public record UpdateUserInfoResponse(Guid Id);

public class UpdateUserInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/auth/info", ([FromBody] UpdateUserInfoRequest request, UpdateUserInfoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Auth");
}

public class UpdateUserInfoHandler(
    UserManager<UserEntity> userManager,
    ICurrentUser currentUser) : IRequestHandler<UpdateUserInfoRequest, UpdateUserInfoResponse>
{
    public async Task<UpdateUserInfoResponse> Handle(UpdateUserInfoRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

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

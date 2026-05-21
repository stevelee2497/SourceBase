using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/register", ([FromBody] RegisterRequest request, IRequestHandler<RegisterRequest, NoContent> handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RegisterHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper) : IRequestHandler<RegisterRequest, NoContent>
{
    public async Task<NoContent> Handle(RegisterRequest request, CancellationToken ct)
    {
        var user = new UserEntity { Email = request.Email, UserName = request.Email };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(createResult.Errors.First().Description);

        var persistedUser = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (persistedUser.EmailConfirmed)
            throw new BadRequestException("Email already confirmed");

        var otp = OtpHelper.Generate();
        persistedUser.OtpCode = otp;
        await userManager.UpdateAsync(persistedUser);
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>");

        return TypedResults.NoContent();
    }
}

public record RegisterRequest(string Email, string Password);

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

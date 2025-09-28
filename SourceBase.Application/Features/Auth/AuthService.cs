using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;
using System.Text;

namespace SourceBase.Application.Features.Auth;

[ScopedDependency<IAuthService>]
public class AuthService(IUserContext userContext, IDbContext dbContext, UserManager<UserEntity> userManager, AppSettings appSettings, IEmailHelper emailHelper) : IAuthService
{
    public async Task RegisterAsync(RegisterRequest request)
    {
        // Create a new user
        var user = new UserEntity
        {
            Email = request.Email,
            UserName = request.Email,
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ApiInternalException(result.Errors.First().Description);
        }

        await SendConfirmationEmailAsync(user);
    }

    public async Task LoginAsync(LoginRequest login)
    {
        await userContext.LoginAsync(login.Email, login.Password);
    }

    public async Task RefreshAsync(RefreshTokenRequest refreshToken)
    {
        await userContext.RefreshAsync(refreshToken.Token);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not { } user)
        {
            throw new UnAuthorizedException();
        }

        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(request.Code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);
        if (!result.Succeeded)
        {
            throw new UnAuthorizedException();
        }

        await userManager.AddToRoleAsync(user, Roles.User);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetPasswordUrl = $"{appSettings.ApiUrl}/resetPassword?email={request.Email}&code={code}";
        if (resetPasswordUrl == null)
        {
            throw new Exception("Couldn't send email");
        }

        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Click <a href='{resetPasswordUrl}'>here</a> to reset your password.");
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
        {
            throw new ApiInternalException("Email already confirmed");
        }

        await SendConfirmationEmailAsync(user);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        var decodedCode = Encoding.UTF8.GetString(Base64UrlHelper.Base64UrlDecode(request.Code));
        var result = await userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ApiInternalException(result.Errors.First().Description);
        }
    }

    public async Task<UserInfoResponse> GetUserInfoAsync()
    {
        return await dbContext.Users
            .Include(x => x.Roles)
            .Where(x => x.Id == userContext.CurrentUserId)
            .Select(x => new UserInfoResponse(x.Id, x.Email, x.FirstName, x.LastName, x.PhoneNumber, x.Roles.Select(ur => ur.Name!)))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException();
    }

    public async Task UpdateUserInfoAsync(UserInfoUpdateRequest userInfo)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userContext.CurrentUserId) ?? throw new NotFoundException();

        user.FirstName = userInfo.FirstName;
        user.LastName = userInfo.LastName;

        await dbContext.SaveChangesAsync();
    }

    private async Task SendConfirmationEmailAsync(UserEntity user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = Base64UrlHelper.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmEmailUrl = $"{appSettings.ApiUrl}/confirmEmail?email={user.Email}&code={code}";
        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
    }
}

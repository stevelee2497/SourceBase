namespace SourceBase.Application.Features.Auth;

public interface IAuthService
{
    Task LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task RefreshAsync(RefreshTokenRequest request);
    Task<UserInfoResponse> GetUserInfoAsync();
    Task UpdateUserInfoAsync(UserInfoUpdateRequest request);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

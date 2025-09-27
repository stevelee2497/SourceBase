namespace Application.Features.Auth;

public interface IAuthService
{
    Task LoginAsync(LoginRequest login);
    Task<string> RegisterAsync(RegisterRequest registration);
    Task RefreshAsync(RefreshTokenRequest refreshToken);
    Task<UserInfoResponse> GetUserInfoAsync();
    Task UpdateUserInfoAsync(UserInfoUpdateRequest userInfo);
    Task ConfirmEmailAsync(string userId, string code);
}

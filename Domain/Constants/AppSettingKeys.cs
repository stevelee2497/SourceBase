namespace Domain.Constants;

public static class AppSettingKeys
{
    public const string ConnectionString = "name=ConnectionStrings:DefaultConnection";
    public const string BearerTokenExpiration = "BearerTokenOptions:BearerTokenExpiration";
    public const string RefreshTokenExpiration = "BearerTokenOptions:RefreshTokenExpiration";
}

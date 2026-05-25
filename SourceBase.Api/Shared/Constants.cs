namespace SourceBase.Api.Shared;

public static class Constants
{
    public const string CorsDefaultPolicy = "AllowAll";
    public const string CorsCustomPolicy = "AllowedSpecificOrigins";
    public const string BearerScheme = "Identity.Bearer";
    public const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
namespace SourceBase.Application.Shared;

public static class Constants
{
    public const string BearerScheme = "Identity.Bearer";
    public const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";
    public const string GeneralRateLimitPolicy = "general";
    public const string StrictRateLimitPolicy = "strict";
    public const string HttpClientName = "SourceBaseHttpClient";
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class CacheKeys
{
    public const string UserInfo = "user-info:{id:guid}";
    public const string WalletSummary = "wallet-summary:{id:guid}";
    public const string GoldPriceSummary = "gold-price-summary";
    public const string DataProtectionKeys = "data-protection-keys";
}
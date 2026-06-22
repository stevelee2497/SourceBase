namespace SourceBase.Application.Shared;

public class AppSettings
{
    public string AdminEmail { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
    public TimeSpan OtpTokenExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AccessTokenExpiration { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan RefreshTokenExpiration { get; set; } = TimeSpan.FromDays(30);
    public string SendGridApiKey { get; set; } = null!;
    public string SendGridAccountOwner { get; set; } = null!;
    public R2Settings R2 { get; set; } = new();
    public bool RedisEnabled { get; set; }
    public BackgroundJobSettings BackgroundJobSettings { get; set; } = new();
    public RateLimitSettings RateLimitSettings { get; set; } = new();
}

public class R2Settings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ServiceURL { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 10;
}

public class BackgroundJobSettings
{
    public bool Enabled { get; set; } = true;
    public TimeSpan GoldPriceScrapingInterval { get; set; } = TimeSpan.FromHours(1);
}

public class RateLimitSettings
{
    public int GeneralPermitLimit { get; set; } = 100;
    public int GeneralWindowSeconds { get; set; } = 60;
    public int StrictPermitLimit { get; set; } = 10;
    public int StrictWindowSeconds { get; set; } = 60;
}
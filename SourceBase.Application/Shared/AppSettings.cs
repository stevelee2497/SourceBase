namespace SourceBase.Application.Shared;

public class AppSettings
{
    public string AdminEmail { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
    public int OtpTokenExpirationMinutes { get; set; } = 15;
    public string WebUrl { get; set; } = null!;
    public string SendGridApiKey { get; set; } = null!;
    public string SendGridAccountOwner { get; set; } = null!;
    public R2Settings R2 { get; set; } = new();
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

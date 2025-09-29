namespace SourceBase.Application.Common;

public class AppSettings
{
    public string AdminEmail { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
    public string ApiUrl { get; set; } = null!;
    public string SendGridApiKey { get; set; } = null!;
    public string SendGridAccountOwner { get; set; } = null!;
}

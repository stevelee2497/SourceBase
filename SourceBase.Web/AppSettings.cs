namespace SourceBase.Web;

public class AppSettings
{
    public List<PageConfig> Pages { get; set; } = [];
};

public class PageConfig
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public string Icon { get; set; } = string.Empty;
    public List<PageConfig> SubPages { get; set; } = [];
}

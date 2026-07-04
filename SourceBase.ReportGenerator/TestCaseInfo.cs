namespace SourceBase.ReportGenerator;

public sealed class TestCaseInfo
{
    public required string Id { get; init; }
    public required string Summary { get; init; }
    public required string DisplayName { get; init; }
    public string Outcome { get; set; } = "NotRun";
}

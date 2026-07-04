namespace SourceBase.ReportGenerator;

public sealed class EndpointInfo
{
    public required string Feature { get; init; }
    public required string Name { get; init; }
    public required string Route { get; init; }
    public required string Auth { get; init; }
    public required string UseCase { get; init; }
    public required string[] Description { get; init; }
    public required string ClassFullName { get; init; }
    public List<TestCaseInfo> TestCases { get; } = [];

    public static EndpointInfo FromAttribute(object attribute, string classFullName)
    {
        var type = attribute.GetType();
        string GetString(string property) => (string)type.GetProperty(property)!.GetValue(attribute)!;
        var description = (string[])type.GetProperty("Description")!.GetValue(attribute)!;

        return new EndpointInfo
        {
            Feature = GetString("Feature"),
            Name = GetString("Name"),
            Route = GetString("Route"),
            Auth = GetString("Auth"),
            UseCase = GetString("UseCase"),
            Description = description,
            ClassFullName = classFullName,
        };
    }
}

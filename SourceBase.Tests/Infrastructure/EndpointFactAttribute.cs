namespace SourceBase.Tests.Infrastructure;

/// <summary>
/// Documents the endpoint under test for an entire test class — the narrative that
/// used to live once per endpoint in docs/features/*.md. Applied once on the class;
/// every [Fact(DisplayName = "ID: summary")] method inside is grouped under this
/// endpoint in the generated report, with ID and summary parsed from the DisplayName.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EndpointFactAttribute : Attribute
{
    /// <summary>e.g. "Auth" — groups endpoints into report sections (was the .md filename)</summary>
    public required string Feature { get; init; }

    /// <summary>e.g. "Login"</summary>
    public required string Name { get; init; }

    /// <summary>e.g. "POST /api/auth/login"</summary>
    public required string Route { get; init; }

    /// <summary>e.g. "Anonymous", "Admin only"</summary>
    public string Auth { get; init; } = "Anonymous";

    /// <summary>The "As a ... I want ... so that ..." user story.</summary>
    public required string UseCase { get; init; }

    /// <summary>Numbered steps, same as the current Description section.</summary>
    public required string[] Description { get; init; }
}

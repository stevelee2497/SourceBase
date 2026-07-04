using System.Reflection;
using System.Xml.Linq;
using SourceBase.ReportGenerator;
using Xunit;

var trxPath = GetArg(args, "--trx") ?? throw new ArgumentException("--trx is required");
var assemblyPath = GetArg(args, "--test-assembly") ?? throw new ArgumentException("--test-assembly is required");
var outputPath = GetArg(args, "--output") ?? "test-report.html";

var assemblyDir = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
AppDomain.CurrentDomain.AssemblyResolve += (_, resolveArgs) =>
{
    var candidate = Path.Combine(assemblyDir, new AssemblyName(resolveArgs.Name).Name + ".dll");
    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
};

var assembly = Assembly.LoadFrom(assemblyPath);
var endpointFactType = assembly.GetType("SourceBase.Tests.Infrastructure.EndpointFactAttribute")
    ?? throw new InvalidOperationException("EndpointFactAttribute not found in test assembly.");

var endpoints = new List<EndpointInfo>();
foreach (var type in GetLoadableTypes(assembly))
{
    if (type is null) continue;
    var endpointAttr = type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == endpointFactType);
    if (endpointAttr is null) continue;

    var endpoint = EndpointInfo.FromAttribute(endpointAttr, type.FullName!);

    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
    {
        var fact = method.GetCustomAttributes().OfType<FactAttribute>().FirstOrDefault();
        if (fact is null) continue;

        var displayName = fact.DisplayName ?? method.Name;
        var parts = displayName.Split(':', 2);
        var id = parts[0].Trim();
        var summary = parts.Length > 1 ? parts[1].Trim() : displayName;

        endpoint.TestCases.Add(new TestCaseInfo { Id = id, Summary = summary, DisplayName = displayName });
    }

    if (endpoint.TestCases.Count > 0)
        endpoints.Add(endpoint);
}

var trx = XDocument.Load(trxPath);
XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
var results = trx.Descendants(ns + "UnitTestResult")
    .GroupBy(r => r.Attribute("testName")!.Value)
    .ToDictionary(g => g.Key, g => (Outcome: g.First().Attribute("outcome")!.Value, Duration: g.First().Attribute("duration")?.Value));

foreach (var endpoint in endpoints)
{
    foreach (var tc in endpoint.TestCases)
        tc.Outcome = results.TryGetValue(tc.DisplayName, out var r) ? r.Outcome : "NotRun";
}

var html = HtmlRenderer.Render(endpoints);
File.WriteAllText(outputPath, html);

var allCases = endpoints.SelectMany(e => e.TestCases).ToList();
var total = allCases.Count;
var passed = allCases.Count(t => t.Outcome == "Passed");
Console.WriteLine($"Report written to {outputPath}. {passed}/{total} passed.");

return 0;

static Type?[] GetLoadableTypes(Assembly asm)
{
    try { return asm.GetTypes(); }
    catch (ReflectionTypeLoadException ex) { return ex.Types; }
}

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

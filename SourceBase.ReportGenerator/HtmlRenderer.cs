using System.Net;
using System.Text;

namespace SourceBase.ReportGenerator;

public static class HtmlRenderer
{
    public static string Render(List<EndpointInfo> endpoints)
    {
        var allCases = endpoints.SelectMany(e => e.TestCases).ToList();
        var total = allCases.Count;
        var passed = allCases.Count(t => t.Outcome == "Passed");
        var failed = allCases.Count(t => t.Outcome == "Failed");
        var generatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>SourceBase Test Report</title><style>").Append(Css).Append("</style></head><body>");

        sb.Append("<header class=\"page-header\">");
        sb.Append("<h1>SourceBase Test Report</h1>");
        sb.Append($"<p class=\"generated\">Generated {Html(generatedAt)}</p>");
        var badgeClass = failed == 0 ? "badge-pass" : "badge-fail";
        sb.Append($"<div class=\"summary-badge {badgeClass}\">{passed}/{total} passed</div>");
        sb.Append("</header>");

        foreach (var featureGroup in endpoints.GroupBy(e => e.Feature).OrderBy(g => g.Key))
        {
            sb.Append("<details class=\"feature\" open>");
            sb.Append($"<summary class=\"feature-title\">{Html(featureGroup.Key)}</summary>");

            foreach (var endpoint in featureGroup.OrderBy(e => e.Name))
                RenderEndpoint(sb, endpoint);

            sb.Append("</details>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void RenderEndpoint(StringBuilder sb, EndpointInfo endpoint)
    {
        var endpointFailed = endpoint.TestCases.Any(t => t.Outcome != "Passed");
        sb.Append("<section class=\"endpoint\">");
        sb.Append("<div class=\"endpoint-header\">");
        sb.Append($"<h3>{Html(endpoint.Name)}</h3>");
        sb.Append($"<code class=\"route\">{Html(endpoint.Route)}</code>");
        sb.Append($"<span class=\"auth-badge\">{Html(endpoint.Auth)}</span>");
        sb.Append("</div>");

        sb.Append($"<blockquote class=\"use-case\">{Html(endpoint.UseCase)}</blockquote>");

        sb.Append("<ol class=\"description\">");
        foreach (var step in endpoint.Description)
            sb.Append($"<li>{Html(step)}</li>");
        sb.Append("</ol>");

        sb.Append("<table class=\"test-cases\"><thead><tr><th>ID</th><th>Summary</th><th>Status</th></tr></thead><tbody>");
        foreach (var tc in endpoint.TestCases.OrderBy(t => t.Id))
        {
            var rowClass = tc.Outcome == "Failed" ? " class=\"row-fail\"" : "";
            var statusText = tc.Outcome switch
            {
                "Passed" => "✅ Pass",
                "NotRun" or "NotExecuted" => "⚪ Not Run",
                "Failed" => "❌ Fail",
                _ => $"⚪ {tc.Outcome}",
            };
            sb.Append($"<tr{rowClass}><td>{Html(tc.Id)}</td><td>{Html(tc.Summary)}</td><td>{statusText}</td></tr>");
        }
        sb.Append("</tbody></table>");

        _ = endpointFailed;
        sb.Append("</section>");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private const string Css = """
        :root { color-scheme: light; }
        * { box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; margin: 0; background: #f6f7f9; color: #1a1a1a; }
        .page-header { padding: 2rem; background: #1a2332; color: #fff; }
        .page-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; }
        .generated { margin: 0; opacity: 0.7; font-size: 0.85rem; }
        .summary-badge { display: inline-block; margin-top: 0.75rem; padding: 0.35rem 0.9rem; border-radius: 999px; font-weight: 600; font-size: 0.9rem; }
        .badge-pass { background: #16a34a; color: #fff; }
        .badge-fail { background: #dc2626; color: #fff; }
        .feature { margin: 1rem 2rem; background: #fff; border: 1px solid #e2e5ea; border-radius: 8px; }
        .feature-title { padding: 1rem 1.25rem; font-size: 1.15rem; font-weight: 700; cursor: pointer; }
        .endpoint { padding: 1rem 1.25rem 1.5rem; border-top: 1px solid #eceef1; }
        .endpoint-header { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
        .endpoint-header h3 { margin: 0; font-size: 1.05rem; }
        .route { background: #eef1f6; padding: 0.15rem 0.5rem; border-radius: 4px; font-size: 0.85rem; }
        .auth-badge { background: #fde68a; color: #713f12; padding: 0.1rem 0.5rem; border-radius: 4px; font-size: 0.75rem; font-weight: 600; }
        .use-case { margin: 0.75rem 0; padding: 0.5rem 1rem; border-left: 3px solid #94a3b8; color: #475569; font-style: italic; }
        .description { margin: 0.5rem 0; padding-left: 1.5rem; color: #334155; font-size: 0.9rem; }
        .test-cases { width: 100%; border-collapse: collapse; margin-top: 0.75rem; font-size: 0.88rem; }
        .test-cases th, .test-cases td { text-align: left; padding: 0.4rem 0.6rem; border-bottom: 1px solid #eceef1; }
        .test-cases th { color: #64748b; font-weight: 600; }
        .row-fail { background: #fef2f2; }
        """;
}

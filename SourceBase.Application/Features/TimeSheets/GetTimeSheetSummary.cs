using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.TimeSheets;

public record GetTimeSheetSummaryRequest(int Year, int Month);

public record TimeSheetDaySummary(DateOnly Date, decimal TotalHours, List<string> Projects);

public record GetTimeSheetSummaryResponse(List<TimeSheetDaySummary> Days);

public class GetTimeSheetSummaryEndpoint : IEndpoint
{
    public const string Route = "time-sheets/summary";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, ([AsParameters] GetTimeSheetSummaryRequest request, GetTimeSheetSummaryHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TimeSheets");
}

public class GetTimeSheetSummaryHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetTimeSheetSummaryRequest, GetTimeSheetSummaryResponse>
{
    public async Task<GetTimeSheetSummaryResponse> Handle(GetTimeSheetSummaryRequest request, CancellationToken ct)
    {
        var entries = await dbContext.TimeSheets
            .Where(x => x.UserId == currentUser.UserId && x.Date.Year == request.Year && x.Date.Month == request.Month)
            .Select(x => new { x.Date, x.Project, x.Hours })
            .ToListAsync(ct);

        var days = entries
            .GroupBy(x => x.Date)
            .Select(g => new TimeSheetDaySummary(
                g.Key,
                g.Sum(x => x.Hours),
                [.. g.Select(x => x.Project).Order()]))
            .OrderBy(x => x.Date)
            .ToList();

        return new GetTimeSheetSummaryResponse(days);
    }
}

public class GetTimeSheetSummaryRequestValidator : AbstractValidator<GetTimeSheetSummaryRequest>
{
    public GetTimeSheetSummaryRequestValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

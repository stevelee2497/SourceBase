using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.TimeSheets;

public record TimeSheetItem(DateOnly Date, string Project, decimal Hours);

public record CreateTimeSheetRequest(List<TimeSheetItem> Items);

public record CreateTimeSheetResponse(List<Guid> Ids);

public class CreateTimeSheetEndpoint : IEndpoint
{
    public const string Route = "time-sheets";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTimeSheetRequest request, CreateTimeSheetHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TimeSheets");
}

public class CreateTimeSheetHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTimeSheetRequest, CreateTimeSheetResponse>
{
    public async Task<CreateTimeSheetResponse> Handle(CreateTimeSheetRequest request, CancellationToken ct)
    {
        var ids = new List<Guid>();

        foreach (var item in request.Items)
        {
            var existing = await dbContext.TimeSheets
                .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId && x.Date == item.Date && x.Project == item.Project, ct);

            if (existing is not null)
            {
                existing.Hours = item.Hours;
                ids.Add(existing.Id);
            }
            else
            {
                var entity = new TimeSheetEntity
                {
                    Date = item.Date,
                    Project = item.Project,
                    Hours = item.Hours,
                    UserId = currentUser.UserId,
                };
                dbContext.TimeSheets.Add(entity);
                ids.Add(entity.Id);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return new CreateTimeSheetResponse(ids);
    }
}

public class CreateTimeSheetRequestValidator : AbstractValidator<CreateTimeSheetRequest>
{
    public CreateTimeSheetRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Project).NotEmpty();
            item.RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(8);
            item.RuleFor(x => x.Date).NotEmpty();
        });
    }
}

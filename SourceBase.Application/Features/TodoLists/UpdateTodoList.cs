using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.TodoLists;

public record UpdateTodoListRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Name);

public record UpdateTodoListResponse(Guid Id);

public class UpdateTodoListEndpoint : IEndpoint
{
    public const string Route = "todo-lists/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateTodoListRequest body, UpdateTodoListHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .WithTags("TodoLists");
}

public class UpdateTodoListHandler(IDbContext dbContext) : IRequestHandler<UpdateTodoListRequest, UpdateTodoListResponse>
{
    public async Task<UpdateTodoListResponse> Handle(UpdateTodoListRequest request, CancellationToken ct)
    {
        var list = await dbContext.TodoLists.FindAsync([request.Id], ct)!;

        list!.Name = request.Name ?? list.Name;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateTodoListResponse(list.Id);
    }
}

public class UpdateTodoListRequestValidator : AbstractValidator<UpdateTodoListRequest>
{
    public UpdateTodoListRequestValidator(IDbContext dbContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) =>
            {
                var list = await dbContext.TodoLists.FindAsync([id], ct);
                return list is not null && list.UserId == currentUser.UserId;
            })
            .WithMessage("Todo list not found.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
    }
}

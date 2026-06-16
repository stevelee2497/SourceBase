using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Application.Shared;
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

public class UpdateTodoListHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateTodoListRequest, UpdateTodoListResponse>
{
    public async Task<UpdateTodoListResponse> Handle(UpdateTodoListRequest request, CancellationToken ct)
    {
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        list.Name = request.Name ?? list.Name;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateTodoListResponse(list.Id);
    }
}

public class UpdateTodoListRequestValidator : AbstractValidator<UpdateTodoListRequest>
{
    public UpdateTodoListRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
    }
}

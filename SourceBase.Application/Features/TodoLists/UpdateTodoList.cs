using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.TodoLists;

public record UpdateTodoListRequest([property: SwaggerIgnore] Guid Id, string Name);

public record UpdateTodoListResponse(Guid Id);

public class UpdateTodoListEndpoint : IEndpoint
{
    public const string Route = "todo-lists/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateTodoListRequest body, UpdateTodoListHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .WithTags("TodoLists");
}

public class UpdateTodoListHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateTodoListRequest, UpdateTodoListResponse>
{
    public async Task<UpdateTodoListResponse> Handle(UpdateTodoListRequest request, CancellationToken ct)
    {
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        list.Name = request.Name;
        await dbContext.SaveChangesAsync(ct);
        return new UpdateTodoListResponse(list.Id);
    }
}

public class UpdateTodoListRequestValidator : AbstractValidator<UpdateTodoListRequest>
{
    public UpdateTodoListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

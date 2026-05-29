using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.TodoLists;

public record CreateTodoListRequest(string Name);

public record CreateTodoListResponse(Guid Id);

public class CreateTodoListEndpoint : IEndpoint
{
    public const string Route = "todo-lists";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTodoListRequest request, CreateTodoListHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("TodoLists");
}

public class CreateTodoListHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CreateTodoListRequest, CreateTodoListResponse>
{
    public async Task<CreateTodoListResponse> Handle(CreateTodoListRequest request, CancellationToken ct)
    {
        var list = new TodoListEntity
        {
            Name = request.Name,
            UserId = currentUser.UserId,
        };
        dbContext.TodoLists.Add(list);
        await dbContext.SaveChangesAsync(ct);
        return new CreateTodoListResponse(list.Id);
    }
}

public class CreateTodoListRequestValidator : AbstractValidator<CreateTodoListRequest>
{
    public CreateTodoListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

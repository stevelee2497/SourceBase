---
name: coding
description: "Architecture, conventions, and Blazor patterns for the SourceBase project. Use when writing or reviewing feature code, endpoints, handlers, entities, or Blazor components."
trigger: /coding
---

# /coding

Project-specific coding conventions and architecture for SourceBase.

## Commands

```sh
# Development
dotnet run --project SourceBase.AppHost   # Aspire dashboard at :15017 (logs/traces/metrics)
sh run.sh                                  # API + Web without Aspire

# Build & test
dotnet build
dotnet test
dotnet test --filter "ClassName.MethodName"

# Migrations (always both steps together)
sh cmd-migration-add.sh <Name>
sh cmd-migration-update-db.sh
```

## Architecture

Clean Architecture with Vertical Slice features. Four projects with strict dependency direction: `Api` → `Application` ← `Infrastructure`, all referencing `Domain`.

```
SourceBase.Domain/         # Pure POCO entities (BaseAuditableEntity, ...)
SourceBase.Application/    # Features (use cases), interfaces, shared logic
SourceBase.Infrastructure/ # EF Core, implementations, migrations (PostgreSQL)
SourceBase.Api/            # HTTP entry point — wires AddApplication() + AddInfrastructure()
```

Features live in `SourceBase.Application/Features/` — one file per use case containing request record, response record, endpoint, handler, and validator. No MediatR, no controllers.

```csharp
public record CreateTodoRequest(DateOnly Date, string Title, TodoItemStatus Status);

public record CreateTodoResponse(Guid Id);

public class CreateTodoEndpoint : IEndpoint
{
    public const string Route = "todos";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext) : IRequestHandler<CreateTodoRequest, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        var entity = new TodoItemEntity { Title = request.Title };
        dbContext.TodoItems.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return new CreateTodoResponse(entity.Id);
    }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
    }
}
```

**Key rules:**

- `IEndpoint` and `IRequestHandler<TRequest, TResponse>` are auto-discovered via `AddApplication()` — no manual registration.
- All endpoints are mounted under `/api` with `RequireAuthorization()` by default; use `.AllowAnonymous()` to opt out.
- Keep `MapEndpoint` chains on separate lines: `.MapXxx(...)`, then `.AllowAnonymous()` / `.RequireAuthorization(...)`, then `.WithTags(...)`.
- Record/handler/constructor parameters on one line; avoid multi-line parameter lists.
- For update endpoints the `Id` is a route parameter, marked `[property: SwaggerIgnore]` in the request record to hide it from the OpenAPI schema.
- DI wiring: `AddApplication()` in `SourceBase.Application/DependencyInjection.cs`; `AddInfrastructure()` in `SourceBase.Infrastructure/DependencyInjection.cs`.
- Interfaces belong in `SourceBase.Application/Shared/Interfaces/`; implementations in `SourceBase.Infrastructure/Implementations/`.
- Pagination: define `OrderBy` enums per feature and use `PagingRequest` base class. Apply `.PaginateAsync()` on `IQueryable`.

## Conventions

**Errors** — throw typed exceptions; `GlobalExceptionMiddleware` maps them:

- `NotFoundException` → 404 · `UnAuthorizedException` → 401 · `ForbiddenException` → 403
- `BadRequestException` → 400 · `ValidationException` → 400 (field errors) · `ApiInternalException` → 500

**Entities** — inherit `BaseAuditableEntity` (`Id`, `CreatedOn/By`, `UpdatedOn/By`). Audit fields set automatically by `ApplicationDbContextAuditInterceptor` — never set manually. Enums stored as strings via `EnumToStringConverter`.

**Config** — add new settings to `AppSettings.cs` and `appsettings.json`. Inject as `IOptions<AppSettings>` or singleton `AppSettings`.

**Logging** — Serilog outputs CLEF JSON to console and `Logs/log-.clef` (daily rolling), enriched with `TraceId`, `SpanId`, `MachineName`, `EnvironmentName`. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to forward to an OTLP collector.

## Blazor (`SourceBase.Web`)

Never use inline lambdas for event handlers or component parameters — use named delegates:

```csharp
// void with loop-captured param → Action
private Action OpenEdit(T item) => () => { _editing = item; _showForm = true; };
// @onclick="OpenEdit(item)"

// async Task with loop-captured param → Func<Task>
private Func<Task> SelectItem(Guid id) => async () => { _selectedId = id; await LoadAsync(); };
// @onclick="SelectItem(item.Id)"

// multi-statement inline → named method
private void CancelDelete() { _showDelete = false; _deleting = null; }
// OnCancel="CancelDelete"
```

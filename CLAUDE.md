# CLAUDE.md

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
SourceBase.Domain/        # Pure POCO entities (BaseAuditableEntity, ...)
SourceBase.Application/   # Features (use cases), interfaces, shared logic
SourceBase.Infrastructure/ # EF Core, implementations, migrations (PostgreSQL)
SourceBase.Api/           # HTTP entry point — wires AddApplication() + AddInfrastructure()
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

- `IEndpoint` and `IRequestHandler<TRequest, TResponse>` implementations are auto-discovered via `AddApplication()` — no manual registration needed.
- All endpoints are mounted under `/api` with `RequireAuthorization()` by default; use `.AllowAnonymous()` to opt out.
- Keep `MapEndpoint` chains on separate lines: `.MapXxx(...)`, then `.AllowAnonymous()` / `.RequireAuthorization(...)`, then `.WithTags(...)`.
- Record/handler/constructor parameters on one line; avoid multi-line parameter lists.
- For update endpoints the `Id` is a route parameter, marked `[property: SwaggerIgnore]` in the request record to hide it from the OpenAPI schema.
- DI wiring: `AddApplication()` in `SourceBase.Application/DependencyInjection.cs`; `AddInfrastructure()` in `SourceBase.Infrastructure/DependencyInjection.cs`; thin `Program.Configurations.cs` for HTTP-layer config.
- Interfaces belong in `SourceBase.Application/Shared/Interfaces/`; implementations belong in `SourceBase.Infrastructure/Implementations/`.
- Pagination: Define OrderBy enums per feature (e.g. `TransactionOrderBy`) and use `PagingRequest` base class for common paging params (`Page`, `Limit`, `Order`, `OrderBy`). Using `.PaginateAsync()` extension method on IQueryable applies sorting and pagination based on those params.
- Use primary constructors for handlers and services to keep code concise and avoid boilerplate.

## Conventions

**Errors** — throw typed exceptions; `GlobalExceptionMiddleware` maps them to a `{ TraceId, Code, Message, Errors }` JSON response:

- `NotFoundException` → 404 · `UnAuthorizedException` → 401 · `ForbiddenException` → 403
- `BadRequestException` → 400 · `ValidationException` → 400 (with field errors) · `ApiInternalException` → 500

**Entities** — inherit `BaseAuditableEntity` (`Id`, `CreatedOn/By`, `UpdatedOn/By`). Audit fields are set automatically by `ApplicationDbContextAuditInterceptor` — never set them manually. Enums stored as strings via `EnumToStringConverter`.

**Config** — add new settings to `AppSettings.cs` and `appsettings.json`. Injected as `IOptions<AppSettings>` or directly as a singleton `AppSettings`.

**Logging** — Serilog outputs CLEF JSON to console and `Logs/log-.clef` (daily rolling). Every log entry is enriched with `TraceId`, `SpanId`, `MachineName`, and `EnvironmentName`. HTTP request logs are emitted by `UseSeriLog()`. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to forward to an OTLP collector.

## Testing

Integration tests in `SourceBase.Tests/` with xUnit + FluentAssertions + `WebApplicationFactory`. Test classes mirror `Features/` structure (one class per endpoint).

**Infrastructure:**

- `WebAppFactory` — spins up the full app with an isolated in-memory SQLite database per test run, seeded with an admin user (`AdminEmail` / `AdminPassword` from config).
- `CreateAuthorizedClient()` — returns an `HttpClient` pre-authorized as the seeded admin.
- `GetLatestEmailCodeAsync(email)` — queries `db.Emails` for the latest code sent to that address. Use after any endpoint that sends an email (register, forgot-password). Never generate tokens directly.
- `ConfirmEmailAsync(client, email)` — convenience wrapper: reads code from DB and POSTs to `/api/auth/confirmEmail`.
- Avoid calling `WithDbContextAsync` in tests unless asserting on a DB field not exposed by the API response.

**Test structure:**

```csharp
[Fact(DisplayName = "TODOS-CREATE-006: CreateTodo_WithValidTodoListId_ReturnsOk")]
public async Task CreateTodo_WithValidTodoListId_ReturnsOk()
{
    // Arrange
    var client = await factory.CreateAuthorizedClient();
    var listResponse = await client.PostAsJsonAsync("todo-lists", new { name = $"List_{Guid.NewGuid():N}" });
    var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

    // Act
    var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
    {
        date = "2025-06-01",
        title = "Todo in list",
        status = "Open",
        todoListId = list!.Id,
    });

    // Assert
    var todoResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(body.Id));
    var todo = await todoResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
    todo!.CreatedBy.Should().Be(userInfo!.UserName);
    todo.UserId.Should().Be(userInfo.Id);
}
```

- **Naming:** `MethodName_WithCondition_ReturnsExpected`
- **Test case IDs:** `{FEATURE}-{ACTION}-{NNN}` in `DisplayName` (e.g. `TODOS-CREATE-001`)
- All payload data defined inline — no helper methods that hide intent.
- All Api call must use strong typed Route constants (e.g. `CreateTodoEndpoint.Route`) — never hardcoded strings.

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

## Skills

- `/coding` — full architecture, conventions, and Blazor reference
- `/testing` — test infrastructure and patterns
- `/pr-reviewer` — PR review checklist

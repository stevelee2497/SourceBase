# Copilot Instructions

## Commands

```sh
dotnet run --project SourceBase.AppHost   # Recommended: Aspire dashboard at :15017
sh run.sh                                  # API + Web without Aspire
dotnet build
dotnet test
sh cmd-migration-add.sh <Name> && sh cmd-migration-update-db.sh
docker compose up
```

## Architecture

Clean Architecture with Vertical Slice features. Four projects with strict dependency direction: `Api` → `Application` ← `Infrastructure`, all referencing `Domain`.

```
SourceBase.Domain/         # Pure POCO entities (BaseAuditableEntity, ...)
SourceBase.Application/    # Features (use cases), interfaces, shared logic
SourceBase.Infrastructure/ # EF Core, implementations, migrations (PostgreSQL)
SourceBase.Api/            # HTTP entry point — wires AddApplication() + AddInfrastructure()
```

Features live in `SourceBase.Application/Features/` — one file per use case. No MediatR, no controllers.

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

- `IEndpoint` and `IRequestHandler<TRequest, TResponse>` auto-discovered via `AddApplication()` — no manual registration
- DI: `AddApplication()` in `SourceBase.Application/DependencyInjection.cs`; `AddInfrastructure()` in `SourceBase.Infrastructure/DependencyInjection.cs`
- Interfaces in `SourceBase.Application/Shared/Interfaces/`; implementations in `SourceBase.Infrastructure/Implementations/`
- Endpoints mounted under `/api` with `RequireAuthorization()`; use `.AllowAnonymous()` to opt out
- Keep `MapEndpoint` chains on separate lines: `.MapXxx(...)`, `.AllowAnonymous()` / `.RequireAuthorization()`, `.WithTags(...)`
- Record/handler/constructor parameters on one line; avoid multi-line parameter lists
- For update operations, `Id` is a route parameter marked `[property: SwaggerIgnore]` in the request record

## Conventions

**Errors** — throw typed exceptions; `GlobalExceptionMiddleware` maps them to `{ TraceId, Code, Message, Errors }` JSON:

- `NotFoundException` → 404 · `UnAuthorizedException` → 401 · `ForbiddenException` → 403
- `BadRequestException` → 400 · `ValidationException` → 400 · `ApiInternalException` → 500

**Entities** — inherit `BaseAuditableEntity` (`Id`, `CreatedOn/By`, `UpdatedOn/By`). Audit fields set automatically by `ApplicationDbContextAuditInterceptor` — never set manually. Enums stored as strings via `EnumToStringConverter`.

**Config** — add to `AppSettings.cs` (in `SourceBase.Application/Shared/`) + `appsettings.json`. Available as `IOptions<AppSettings>` and singleton.

**DB** — PostgreSQL via EF Core; migrations live in `SourceBase.Infrastructure/Migrations/`. Auto-migrated on startup in Production.

## Testing

Integration tests live in `SourceBase.Tests/` using `xUnit` + `FluentAssertions` + `WebApplicationFactory`.

**Infrastructure**

- `WebAppFactory` — spins up the full app with an isolated in-memory SQLite database per test run. Seeded with an admin user (`AdminEmail` / `AdminPassword`).
- `Utilities` — extension methods: `PostAsJsonAsync`, `PutAsJsonAsync`, `ReadFromJsonAsync` (with enum-string JSON options), `AuthorizeAsync`, `GetAccessTokenAsync`.

**Helpers on `WebAppFactory`**

- `CreateAuthorizedClient()` — creates an `HttpClient` pre-authorized as the seeded admin.
- `GetLatestEmailCodeAsync(email)` — queries `db.Emails` for the most recent email sent to that address and extracts the `code` query param from the link in the body. Use this after any endpoint that sends an email (register, forgotPassword).
- `ConfirmEmailAsync(client, email)` — convenience wrapper: reads the code from the DB and POSTs to `/api/auth/confirmEmail`.
- Avoid calling WithDbContextAsync directly in tests if possible; instead call appropriate apis to get the response data you need. Only use WithDbContextAsync for assertions that require checking the DB entity field that are not exposed via the API response.

**Email testing** — `SendGridEmailHelper` always persists every outbound email to `db.Emails` (`EmailEntity`: `To`, `Subject`, `Body`, `SentOn`) before dispatching via SendGrid. In tests (no API key configured) the email is only saved to the DB. Retrieve codes via `GetLatestEmailCodeAsync` instead of generating tokens directly from `UserManager`.

**Test structure** — every test follows the AAA pattern:

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

**Test case id convention** - `{FEATURE}-{ACTION}-{NNN}` (e.g. `TODOS-CREATE-001`), included in the `DisplayName` of the test for easy identification and traceability to requirements.

**Test isolation and transparency** - All payload data defined in the test body for clarity; no external helper methods that abstract away the intent of the test.

**Test naming** — `MethodName_WithCondition_ReturnsExpected` (e.g. `Login_WithWrongPassword_ReturnsUnauthorized`).

**Feature isolation** — tests for different features (Auth, Todos, etc.) organized into separate test classes and files. Each test class should only focus on one endpoint.

## Blazor

**Event handlers** — never use inline lambdas (`() => Method(param)`) for event handlers or component parameters. Use named delegates instead:

- `void` method with loop-captured parameter → return `Action`:
  ```csharp
  private Action OpenEdit(T item) => () => { _editing = item; _showForm = true; };
  // usage: @onclick="OpenEdit(item)"
  ```
- `async Task` method with loop-captured parameter → return `Func<Task>`:
  ```csharp
  private Func<Task> SelectItem(Guid id) => async () => { _selectedId = id; await LoadAsync(); };
  // usage: @onclick="SelectItem(item.Id)"
  ```
- Multi-statement or single-expression inline lambdas on component parameters (`OnClose`, `OnCancel`, `OnSaved`, etc.) → extract to named `void` methods:
  ```csharp
  private void CancelDelete() { _showDelete = false; _deleting = null; }
  // usage: OnCancel="CancelDelete"
  ```

## PR Review Checklist

Apply this checklist on every pull request review.

### Architecture

- Feature lives in `SourceBase.Application/Features/` as a single file (request, response, endpoint, handler, validator)
- No MediatR, no controllers — uses `IEndpoint` / `IRequestHandler<TRequest, TResponse>`
- Dependency direction respected: `Api` → `Application` ← `Infrastructure` ← `Domain`
- New interfaces in `SourceBase.Application/Shared/Interfaces/`; implementations in `SourceBase.Infrastructure/Implementations/`
- New settings added to both `AppSettings.cs` and `appsettings.json`

### Endpoints & handlers

- `MapEndpoint` chain uses separate lines (`.MapXxx`, then auth, then `.WithTags`)
- Update endpoints: `Id` is a route param marked `[property: SwaggerIgnore]` on the request record
- No multi-line parameter lists on records, handlers, or constructors
- Auth: default `RequireAuthorization()` present, or `.AllowAnonymous()` intentional
- Pagination uses `PagingRequest` base + feature-specific `OrderBy` enum + `.PaginateAsync()`

### Entities & data

- Entity inherits `BaseAuditableEntity`; audit fields (`CreatedOn/By`, `UpdatedOn/By`) never set manually
- Enums stored via `EnumToStringConverter`
- Errors thrown as typed exceptions (`NotFoundException`, `BadRequestException`, etc.) — no raw status codes

### Partial updates

- Partial update handlers use `entity.Field = request.Field ?? entity.Field` (null-coalescing), not if-guards
- Single-field updates extend existing endpoints rather than adding new dedicated endpoints

### Tests

- At least one integration test per new/changed endpoint in `SourceBase.Tests/`
- Test class mirrors `Features/` path
- Test IDs follow `{FEATURE}-{ACTION}-{NNN}` format in `DisplayName`
- Method name follows `MethodName_WithCondition_ReturnsExpected`
- Uses strong-typed route constants (e.g. `CreateTodoEndpoint.Route`) — no hardcoded URL strings
- Email codes retrieved via `GetLatestEmailCodeAsync` — no manually generated tokens
- `WithDbContextAsync` only used when asserting on a DB field not returned by the API

### Blazor

- No inline lambdas on event handlers or component parameters
- Loop-captured params use `Action`-returning or `Func<Task>`-returning methods
- Multi-statement handlers extracted to named methods

---
